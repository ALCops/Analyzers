using System.Collections.Immutable;
using System.Text;
using ALCops.Common.Extensions;
using ALCops.Common.Helpers;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class EventSubscriberNamingPattern : DiagnosticAnalyzer
{
    // The default matches the identifier form the AL Language extension's "Find Event" feature
    // generates verbatim (e.g. "Sales Header_OnAfterValidateEvent_Document Type") so freshly
    // inserted subscribers pass out of the box.
    private const string DefaultTemplate = "{Event Source}_{EventName}[_{Element Name}]";

    // AL identifier length limit enforced by AL304. Suggesting a longer name would just move
    // the violation from LC0098 to AL304, so both the analyzer and the CodeFix stay silent
    // once the derived name would exceed this budget.
    private const int MaxAlIdentifierLength = 120;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.EventSubscriberNamingPattern);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationStartAction(CompilationStart);

    private void CompilationStart(CompilationStartAnalysisContext ctx)
    {
        var settings = ALCopsSettingsProvider.GetSettings(ctx.Compilation.FileSystem);
        var template = string.IsNullOrWhiteSpace(settings.SubscriberNameTemplate)
            ? DefaultTemplate
            : settings.SubscriberNameTemplate!;

        var segments = TemplateParser.Parse(template);
        var acronyms = AcronymRegistry.Create(settings.KnownAcronyms);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeMethod(symbolCtx, segments, acronyms),
            EnumProvider.SymbolKind.Method);
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext ctx,
        IReadOnlyList<TemplateSegment> segments,
        AcronymRegistry acronyms)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IMethodSymbol method)
        {
            return;
        }

        var preferred = TryBuildPreferredFor(method, segments, acronyms);

        if (preferred is null)
        {
            return;
        }

        // Strict single-form: the analyzer accepts exactly the canonical name the template
        // renders. There is no tolerance for alternate casings (e.g. "Vat" vs the canonical
        // "VAT"); the CodeFix rewrites the declaration to the preferred name in one step.
        if (string.Equals(method.Name, preferred, StringComparison.Ordinal))
        {
            return;
        }

        // AL304 guard: the AL compiler rejects identifiers longer than 120 characters, and the
        // reviewer's survey of the W1 codebase confirms this only bites on a handful of
        // outliers. Report nothing (and let the CodeFix skip too) so LC0098 never suggests a
        // name that would trigger AL304.
        if (preferred.Length > MaxAlIdentifierLength)
        {
            return;
        }

        // Collision guard: a codeunit can legally host two subscribers to the same event, and
        // both would compute to the same preferred name. Renaming both at once produces a
        // duplicate-identifier compile error. If the target name already exists (or another
        // subscriber in the same containing type would compute to it), stay silent — the
        // developer has to resolve the disambiguation manually before the rule can help.
        if (WouldCollideInContainingType(method, preferred, segments, acronyms))
        {
            return;
        }

        var properties = ImmutableDictionary<string, string>.Empty
            .Add("PreferredName", preferred);

        // Message uses the quoted form so the suggestion is a valid AL identifier as-shown
        // (e.g. "Sales Header_OnAfterInsertEvent" with quotes when the source contains a space).
        // The Properties dictionary retains the unquoted form; the CodeFix re-quotes via
        // QuoteIdentifierIfNeededWithReflection when constructing the SyntaxToken.
        var preferredForMessage = preferred.QuoteIdentifierIfNeededWithReflection();

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.EventSubscriberNamingPattern,
            method.GetLocation(),
            properties,
            method.Name,
            preferredForMessage));
    }

    private static string? TryBuildPreferredFor(
        IMethodSymbol method,
        IReadOnlyList<TemplateSegment> segments,
        AcronymRegistry acronyms)
    {
        var attribute = method.Attributes
            .FirstOrDefault(a => a.AttributeKind == EnumProvider.AttributeKind.EventSubscriber);

        if ((attribute is null) || (attribute.Arguments.Length < 4))
        {
            return null;
        }

        var referencedObject = attribute.GetReferencedApplicationObject();

        if (referencedObject is null)
        {
            return null;
        }

        var eventName = attribute.Arguments[2].ValueText;

        if (string.IsNullOrEmpty(eventName))
        {
            return null;
        }

        var eventSourceName = referencedObject.Name;
        var elementName = attribute.Arguments[3].ValueText ?? string.Empty;

        return NameBuilder.BuildPreferred(segments, eventSourceName, eventName, elementName, acronyms);
    }

    private static bool WouldCollideInContainingType(
        IMethodSymbol method,
        string preferred,
        IReadOnlyList<TemplateSegment> segments,
        AcronymRegistry acronyms)
    {
        var containingType = method.ContainingType;

        if (containingType is null)
        {
            return false;
        }

        // AL allows method overloading, so we cannot use name comparison to skip 'self':
        // there may legitimately be a sibling with the same name but a different signature.
        // Compare via ISymbol equality instead. The collision check itself stays conservative
        // (any sibling whose name equals 'preferred' is treated as a collision, even when the
        // signatures differ and the overload would technically compile): renaming into an
        // overload set changes semantics and confuses readers, so silence beats a risky fix.
        foreach (var member in containingType.GetMembers())
        {
            if (member is not IMethodSymbol sibling)
            {
                continue;
            }

            if (sibling.Equals(method))
            {
                continue;
            }

            // Case A: an existing method already carries the preferred name.
            if (string.Equals(sibling.Name, preferred, StringComparison.Ordinal))
            {
                return true;
            }

            // Case B: another event subscriber would rename to the same preferred name.
            var siblingPreferred = TryBuildPreferredFor(sibling, segments, acronyms);

            if ((siblingPreferred is not null)
                && string.Equals(siblingPreferred, preferred, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private enum TokenKind { EventSource, EventName, ElementName }

    private abstract class TemplateSegment { }

    private sealed class LiteralSegment : TemplateSegment
    {
        public string Text { get; }
        public LiteralSegment(string text) => Text = text;
    }

    private sealed class TokenSegment : TemplateSegment
    {
        public TokenKind Kind { get; }
        public IdentifierCaseStyle Style { get; }
        public TokenSegment(TokenKind kind, IdentifierCaseStyle style) { Kind = kind; Style = style; }
    }

    private sealed class ConditionalGroupSegment : TemplateSegment
    {
        public IReadOnlyList<TemplateSegment> Children { get; }
        public ConditionalGroupSegment(IReadOnlyList<TemplateSegment> children) => Children = children;
    }

    private static class TemplateParser
    {
        private static readonly Dictionary<string, (TokenKind Kind, IdentifierCaseStyle Style)> KnownPlaceholders =
            new Dictionary<string, (TokenKind, IdentifierCaseStyle)>(StringComparer.Ordinal)
            {
                ["{EventSource}"]  = (TokenKind.EventSource,  IdentifierCaseStyle.Pascal),
                ["{eventSource}"]  = (TokenKind.EventSource,  IdentifierCaseStyle.Camel),
                ["{event_source}"] = (TokenKind.EventSource,  IdentifierCaseStyle.Snake),
                ["{event-source}"] = (TokenKind.EventSource,  IdentifierCaseStyle.Kebab),
                ["{Event Source}"] = (TokenKind.EventSource,  IdentifierCaseStyle.Raw),
                ["{EventName}"]    = (TokenKind.EventName,    IdentifierCaseStyle.Pascal),
                ["{eventName}"]    = (TokenKind.EventName,    IdentifierCaseStyle.Camel),
                ["{event_name}"]   = (TokenKind.EventName,    IdentifierCaseStyle.Snake),
                ["{event-name}"]   = (TokenKind.EventName,    IdentifierCaseStyle.Kebab),
                ["{Event Name}"]   = (TokenKind.EventName,    IdentifierCaseStyle.Raw),
                ["{ElementName}"]  = (TokenKind.ElementName,  IdentifierCaseStyle.Pascal),
                ["{elementName}"]  = (TokenKind.ElementName,  IdentifierCaseStyle.Camel),
                ["{element_name}"] = (TokenKind.ElementName,  IdentifierCaseStyle.Snake),
                ["{element-name}"] = (TokenKind.ElementName,  IdentifierCaseStyle.Kebab),
                ["{Element Name}"] = (TokenKind.ElementName,  IdentifierCaseStyle.Raw),
            };

        public static IReadOnlyList<TemplateSegment> Parse(string template)
        {
            int pos = 0;
            var segments = new List<TemplateSegment>();

            ParseInto(template, segments, ref pos, insideGroup: false);

            return segments;
        }

        private static void ParseInto(string template, List<TemplateSegment> segments, ref int pos, bool insideGroup)
        {
            var literal = new StringBuilder();

            while (pos < template.Length)
            {
                char c = template[pos];

                if (insideGroup && c == ']')
                {
                    if (literal.Length > 0)
                    {
                        segments.Add(new LiteralSegment(literal.ToString()));
                        literal.Clear();
                    }

                    pos++;
                    return;
                }

                if (!insideGroup && c == '[')
                {
                    if (literal.Length > 0)
                    {
                        segments.Add(new LiteralSegment(literal.ToString()));
                        literal.Clear();
                    }

                    pos++;
                    var groupChildren = new List<TemplateSegment>();

                    ParseInto(template, groupChildren, ref pos, insideGroup: true);
                    segments.Add(new ConditionalGroupSegment(groupChildren));

                    continue;
                }

                if (c == '{')
                {
                    if (literal.Length > 0)
                    {
                        segments.Add(new LiteralSegment(literal.ToString()));
                        literal.Clear();
                    }

                    int braceEnd = template.IndexOf('}', pos + 1);

                    if (braceEnd < 0)
                    {
                        segments.Add(new LiteralSegment(template.Substring(pos)));
                        pos = template.Length;

                        return;
                    }

                    var placeholder = template.Substring(pos, braceEnd - pos + 1);

                    if (KnownPlaceholders.TryGetValue(placeholder, out var tokenInfo))
                    {
                        segments.Add(new TokenSegment(tokenInfo.Kind, tokenInfo.Style));
                    }
                    else
                    {
                        segments.Add(new LiteralSegment(placeholder));
                    }

                    pos = braceEnd + 1;

                    continue;
                }

                literal.Append(c);
                pos++;
            }

            if (literal.Length > 0)
            {
                segments.Add(new LiteralSegment(literal.ToString()));
            }
        }
    }

    private static class NameBuilder
    {
        public static string BuildPreferred(
            IReadOnlyList<TemplateSegment> segments,
            string eventSource,
            string eventName,
            string elementName,
            AcronymRegistry acronyms)
        {
            var sb = new StringBuilder();

            AppendPreferred(segments, sb, eventSource, eventName, elementName, acronyms);

            return sb.ToString();
        }

        private static void AppendPreferred(
            IReadOnlyList<TemplateSegment> segments,
            StringBuilder sb,
            string eventSource,
            string eventName,
            string elementName,
            AcronymRegistry acronyms)
        {
            foreach (var segment in segments)
            {
                if (segment is LiteralSegment literal)
                {
                    sb.Append(literal.Text);
                }
                else if (segment is TokenSegment token)
                {
                    var value = TokenValue(token.Kind, eventSource, eventName, elementName);
                    sb.Append(IdentifierNameRenderer.Render(value, token.Style, acronyms));
                }
                else if (segment is ConditionalGroupSegment group)
                {
                    if (AllTokensNonEmpty(group.Children, eventSource, eventName, elementName))
                    {
                        AppendPreferred(group.Children, sb, eventSource, eventName, elementName, acronyms);
                    }
                }
            }
        }

        private static string TokenValue(TokenKind kind, string eventSource, string eventName, string elementName) =>
            kind switch
            {
                TokenKind.EventSource => eventSource,
                TokenKind.EventName => eventName,
                TokenKind.ElementName => elementName,
                _ => string.Empty
            };

        private static bool AllTokensNonEmpty(
            IReadOnlyList<TemplateSegment> segments,
            string eventSource,
            string eventName,
            string elementName)
        {
            foreach (var segment in segments)
            {
                if (segment is TokenSegment token)
                {
                    var value = TokenValue(token.Kind, eventSource, eventName, elementName);

                    if (string.IsNullOrEmpty(value))
                    {
                        return false;
                    }
                }
                else if (segment is ConditionalGroupSegment nested)
                {
                    if (!AllTokensNonEmpty(nested.Children, eventSource, eventName, elementName))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}