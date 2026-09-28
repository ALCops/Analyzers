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
    private const string DefaultTemplate = "{Event Source}_{Event Name}[_{Element Name}]";

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
        var settings = ALCopsSettingsProvider.GetSettings(ctx.Compilation, ctx.CancellationToken);
        var template = string.IsNullOrWhiteSpace(settings.SubscriberNamingPattern)
            ? DefaultTemplate
            : settings.SubscriberNamingPattern!;

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

        var accepted = TryBuildAcceptedFor(method, segments, acronyms);

        if (accepted is null || accepted.Count == 0)
        {
            return;
        }

        // Element [0] is the preferred / canonical spelling ("original casing wins") and is
        // what the CodeFix will suggest. Any additional element is a variant accepted via a
        // KnownAcronyms entry on the same upper-invariant key as an uppercase source word.
        var preferred = accepted[0];

        // Accept if the current name matches any accepted spelling (preferred or an
        // opt-in acronym variant). Ordinal comparison — casing is significant.
        if (accepted.Contains(method.Name, StringComparer.Ordinal))
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

    private static List<string>? TryBuildAcceptedFor(
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

        var eventSource = referencedObject.Name;
        var eventSourceType = referencedObject.Kind.ToString();
        var elementName = attribute.Arguments[3].ValueText ?? string.Empty;
        var isSelfReference = IsSelfReference(
            referencedObject,
            method.GetContainingApplicationObjectTypeSymbol());

        return NameBuilder.BuildAccepted(segments, eventSourceType, eventSource, eventName, elementName, isSelfReference, acronyms);
    }

    private static bool IsSelfReference(
        IApplicationObjectTypeSymbol source,
        IApplicationObjectTypeSymbol? containingObject)
    {
        if (source.IsSameApplicationObject(containingObject))
        {
            return true;
        }

        if (containingObject is IApplicationObjectExtensionTypeSymbol extension)
        {
            return source.IsSameApplicationObject(extension.Target);
        }

        return false;
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
        //
        // Name comparison is case-insensitive (`SemanticFacts.IsSameName`) because AL treats
        // duplicate method identifiers case-insensitively (`AL0018`); an only-case-different
        // sibling would still cause the CodeFix to produce a duplicate-identifier error.
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
            if (SemanticFacts.IsSameName(sibling.Name, preferred))
            {
                return true;
            }

            // Case B: another event subscriber would rename to the same preferred name.
            // We only compare against the sibling's *preferred* spelling (element [0]);
            // the extra accepted variants of the sibling do not create collisions.
            var siblingAccepted = TryBuildAcceptedFor(sibling, segments, acronyms);

            if ((siblingAccepted is not null) && (siblingAccepted.Count > 0)
                && SemanticFacts.IsSameName(siblingAccepted[0], preferred))
            {
                return true;
            }
        }

        return false;
    }

    private enum TokenKind { EventSourceType, EventSource, EventName, ElementName }

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
        public string? SelfReferenceText { get; }
        public TokenSegment(TokenKind kind, IdentifierCaseStyle style, string? selfReferenceText = null)
        {
            Kind = kind;
            Style = style;
            SelfReferenceText = selfReferenceText;
        }
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
#pragma warning disable IDE0055 // Aligned lookup table; the formatter has no aligned-assignment option
                ["{EventSource}"]       = (TokenKind.EventSource,  IdentifierCaseStyle.Pascal),
                ["{eventSource}"]       = (TokenKind.EventSource,  IdentifierCaseStyle.Camel),
                ["{event_source}"]      = (TokenKind.EventSource,  IdentifierCaseStyle.Snake),
                ["{event-source}"]      = (TokenKind.EventSource,  IdentifierCaseStyle.Kebab),
                ["{Event Source}"]      = (TokenKind.EventSource,  IdentifierCaseStyle.Raw),
                ["{EventSourceType}"]   = (TokenKind.EventSourceType, IdentifierCaseStyle.Pascal),
                ["{eventSourceType}"]   = (TokenKind.EventSourceType, IdentifierCaseStyle.Camel),
                ["{event_source_type}"] = (TokenKind.EventSourceType, IdentifierCaseStyle.Snake),
                ["{event-source-type}"] = (TokenKind.EventSourceType, IdentifierCaseStyle.Kebab),
                ["{Event Source Type}"] = (TokenKind.EventSourceType, IdentifierCaseStyle.Raw),
                ["{EventName}"]         = (TokenKind.EventName,    IdentifierCaseStyle.Pascal),
                ["{eventName}"]         = (TokenKind.EventName,    IdentifierCaseStyle.Camel),
                ["{event_name}"]        = (TokenKind.EventName,    IdentifierCaseStyle.Snake),
                ["{event-name}"]        = (TokenKind.EventName,    IdentifierCaseStyle.Kebab),
                ["{Event Name}"]        = (TokenKind.EventName,    IdentifierCaseStyle.Raw),
                ["{ElementName}"]       = (TokenKind.ElementName,  IdentifierCaseStyle.Pascal),
                ["{elementName}"]       = (TokenKind.ElementName,  IdentifierCaseStyle.Camel),
                ["{element_name}"]      = (TokenKind.ElementName,  IdentifierCaseStyle.Snake),
                ["{element-name}"]      = (TokenKind.ElementName,  IdentifierCaseStyle.Kebab),
                ["{Element Name}"]      = (TokenKind.ElementName,  IdentifierCaseStyle.Raw),
#pragma warning restore IDE0055
            };

        public static List<TemplateSegment> Parse(string template)
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

                    if (TryGetTokenInfo(placeholder, out var tokenInfo, out var selfReferenceText))
                    {
                        segments.Add(new TokenSegment(tokenInfo.Kind, tokenInfo.Style, selfReferenceText));
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

        private static bool TryGetTokenInfo(
            string placeholder,
            out (TokenKind Kind, IdentifierCaseStyle Style) tokenInfo,
            out string? selfReferenceText)
        {
            if (KnownPlaceholders.TryGetValue(placeholder, out tokenInfo))
            {
                selfReferenceText = null;
                return true;
            }

            var separatorIndex = placeholder.IndexOf('|');

            if (separatorIndex <= 1 || separatorIndex == placeholder.Length - 2)
            {
                selfReferenceText = null;
                return false;
            }

            var tokenName = placeholder.Remove(separatorIndex, placeholder.Length - separatorIndex - 1);

            if (!KnownPlaceholders.TryGetValue(tokenName, out tokenInfo)
                || tokenInfo.Kind != TokenKind.EventSourceType)
            {
                selfReferenceText = null;
                return false;
            }

            selfReferenceText = placeholder.Substring(separatorIndex + 1, placeholder.Length - separatorIndex - 2);
            return true;
        }
    }

    private static class NameBuilder
    {
        /// <summary>
        /// Returns the set of accepted subscriber names for the given template + inputs.
        /// Element <c>[0]</c> is the preferred / canonical spelling suggested by the CodeFix
        /// ("original casing wins"). Additional elements appear only when a Pascal / non-first
        /// Camel token contains a source word whose upper-invariant key has a registered
        /// acronym with a different casing — those variants are accepted alongside the
        /// preferred spelling. See <see cref="IdentifierNameRenderer.RenderAccepted"/>.
        /// </summary>
        public static List<string> BuildAccepted(
            IReadOnlyList<TemplateSegment> segments,
            string eventSourceType,
            string eventSource,
            string eventName,
            string elementName,
            bool isSelfReference,
            AcronymRegistry acronyms)
        {
            // Start with a single empty accumulator; extend once per segment. When a segment
            // contributes only one alternative (the common case), the accumulator grows in
            // place with no outer-list allocation.
            var accumulators = new List<StringBuilder>(1) { new StringBuilder() };

            ExtendAccepted(segments, ref accumulators, eventSourceType, eventSource, eventName, elementName, isSelfReference, acronyms);

            // Materialize and dedupe preserving first-seen order so element [0] is preferred.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<string>(accumulators.Count);

            foreach (var sb in accumulators)
            {
                var s = sb.ToString();

                if (seen.Add(s))
                {
                    result.Add(s);
                }
            }

            return result;
        }

        private static void ExtendAccepted(
            IReadOnlyList<TemplateSegment> segments,
            ref List<StringBuilder> accumulators,
            string eventSourceType,
            string eventSource,
            string eventName,
            string elementName,
            bool isSelfReference,
            AcronymRegistry acronyms)
        {
            foreach (var segment in segments)
            {
                if (segment is LiteralSegment literal)
                {
                    foreach (var sb in accumulators)
                    {
                        sb.Append(literal.Text);
                    }
                }
                else if (segment is TokenSegment token)
                {
                    if (token.SelfReferenceText is not null && isSelfReference)
                    {
                        var selfReferenceValue = NamingTokenValue(token, eventSourceType, eventSource, eventName, elementName, isSelfReference);

                        foreach (var sb in accumulators)
                        {
                            sb.Append(selfReferenceValue);
                        }

                        continue;
                    }

                    var referenceValue = NamingTokenValue(token.Kind, eventSourceType, eventSource, eventName, elementName);
                    var alts = IdentifierNameRenderer.RenderAccepted(referenceValue, token.Style, acronyms);

                    if (alts.Count == 1)
                    {
                        var only = alts[0];

                        foreach (var sb in accumulators)
                        {
                            sb.Append(only);
                        }
                    }
                    else
                    {
                        var next = new List<StringBuilder>(accumulators.Count * alts.Count);

                        foreach (var sb in accumulators)
                        {
                            var prefix = sb.ToString();

                            foreach (var alt in alts)
                            {
                                var combined = new StringBuilder(prefix.Length + alt.Length);
                                combined.Append(prefix);
                                combined.Append(alt);
                                next.Add(combined);
                            }
                        }

                        accumulators = next;
                    }
                }
                else if (segment is ConditionalGroupSegment group)
                {
                    if (AllTokensNonEmpty(group.Children, eventSourceType, eventSource, eventName, elementName, isSelfReference))
                    {
                        ExtendAccepted(group.Children, ref accumulators, eventSourceType, eventSource, eventName, elementName, isSelfReference, acronyms);
                    }
                }
            }
        }

        private static string NamingTokenValue(
            TokenSegment token,
            string eventSourceType,
            string eventSource,
            string eventName,
            string elementName,
            bool isSelfReference)
        {
            if (token.SelfReferenceText is not null && isSelfReference)
            {
                return token.SelfReferenceText;
            }

            return NamingTokenValue(token.Kind, eventSourceType, eventSource, eventName, elementName);
        }

        private static string NamingTokenValue(TokenKind kind, string eventSourceType, string eventSource, string eventName, string elementName) =>
            kind switch
            {
                TokenKind.EventSourceType => eventSourceType,
                TokenKind.EventSource => eventSource,
                TokenKind.EventName => eventName,
                TokenKind.ElementName => elementName,
                _ => string.Empty
            };

        private static bool AllTokensNonEmpty(
            IReadOnlyList<TemplateSegment> segments,
            string eventSourceType,
            string eventSource,
            string eventName,
            string elementName,
            bool isSelfReference)
        {
            foreach (var segment in segments)
            {
                if (segment is TokenSegment token)
                {
                    var value = NamingTokenValue(token, eventSourceType, eventSource, eventName, elementName, isSelfReference);

                    if (string.IsNullOrEmpty(value))
                    {
                        return false;
                    }
                }
                else if (segment is ConditionalGroupSegment nested)
                {
                    if (!AllTokensNonEmpty(nested.Children, eventSourceType, eventSource, eventName, elementName, isSelfReference))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
