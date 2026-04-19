using System.Collections.Immutable;
using System.Text.RegularExpressions;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;

using NamingPatternSetting = ALCops.Common.Settings.NamingPattern;

namespace ALCops.LinterCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class NamingPattern : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NamingPattern);

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationStartAction(CompilationStart);

    private void CompilationStart(CompilationStartAnalysisContext ctx)
    {
        var workspacePath = ctx.Compilation.FileSystem?.GetDirectoryPath();
        var settings = ALCopsSettingsProvider.GetSettings(workspacePath);

        List<string>? affixes = null;
        try
        {
            affixes = GetAffixes(ctx.Compilation);
        }
        catch
        {
            // AppSourceCop configuration may not be available in test contexts
        }

        var config = new NamingPatternConfig(settings.NamingPatterns);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeMethod(symbolCtx, config),
            EnumProvider.SymbolKind.Method);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeVariable(symbolCtx, config),
            EnumProvider.SymbolKind.LocalVariable,
            EnumProvider.SymbolKind.GlobalVariable);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeObject(symbolCtx, config, affixes),
            EnumProvider.SymbolKind.Table,
            EnumProvider.SymbolKind.Page,
            EnumProvider.SymbolKind.Codeunit,
            EnumProvider.SymbolKind.Report,
            EnumProvider.SymbolKind.Query,
            EnumProvider.SymbolKind.XmlPort,
            EnumProvider.SymbolKind.Enum,
            EnumProvider.SymbolKind.Interface,
            EnumProvider.SymbolKind.PermissionSet);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeField(symbolCtx, config),
            EnumProvider.SymbolKind.Field);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeEnumValue(symbolCtx, config),
            EnumProvider.SymbolKind.EnumValue);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeAction(symbolCtx, config),
            EnumProvider.SymbolKind.Action);

        ctx.RegisterSymbolAction(
            symbolCtx => AnalyzeControl(symbolCtx, config),
            EnumProvider.SymbolKind.Control);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete() || ctx.Symbol is not IMethodSymbol method)
            return;

        // Skip triggers (platform-defined names)
        if (method.MethodKind != EnumProvider.MethodKind.Method)
            return;

        // Skip interface-implementing methods (can't change name)
        if (method.MethodImplementsInterfaceMethod())
            return;

        // Classify the method to determine which naming target applies
        var target = ClassifyMethod(method);
        CheckName(ctx, method.Name, target, config, GetKindDisplayName(target));

        // Check parameters
        foreach (var parameter in method.Parameters)
        {
            if (string.IsNullOrEmpty(parameter.Name))
                continue;

            CheckNameForSymbol(ctx, parameter, parameter.Name, NamingTarget.Parameter, config, "Parameter");
        }

        // Check return value
        if (method.ReturnValueSymbol is { } returnValue &&
            !string.IsNullOrEmpty(returnValue.Name))
        {
            CheckNameForSymbol(ctx, returnValue, returnValue.Name, NamingTarget.ReturnValue, config, "Return value");
        }
    }

    private static void AnalyzeVariable(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete())
            return;

        CheckName(ctx, ctx.Symbol.Name, NamingTarget.Variable, config, "Variable");
    }

    private static void AnalyzeObject(SymbolAnalysisContext ctx, NamingPatternConfig config,
        List<string>? affixes)
    {
        if (ctx.IsObsolete())
            return;

        var name = StripAffixes(ctx.Symbol.Name, affixes);
        CheckName(ctx, name, NamingTarget.Object, config, "Object");
    }

    private static void AnalyzeField(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete())
            return;

        CheckName(ctx, ctx.Symbol.Name, NamingTarget.Field, config, "Field");
    }

    private static void AnalyzeEnumValue(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete())
            return;

        CheckName(ctx, ctx.Symbol.Name, NamingTarget.EnumValue, config, "Enum value");
    }

    private static void AnalyzeAction(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete())
            return;

        CheckName(ctx, ctx.Symbol.Name, NamingTarget.Action, config, "Action");
    }

    private static void AnalyzeControl(SymbolAnalysisContext ctx, NamingPatternConfig config)
    {
        if (ctx.IsObsolete())
            return;

        CheckName(ctx, ctx.Symbol.Name, NamingTarget.Control, config, "Control");
    }

    private static void CheckName(SymbolAnalysisContext ctx, string name, NamingTarget target,
        NamingPatternConfig config, string kindDisplayName)
    {
        if (string.IsNullOrEmpty(name))
            return;

        var (allowPattern, disallowPattern) = config.GetPatterns(target);

        if (allowPattern is not null)
        {
            if (!TryIsMatch(allowPattern, name))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NamingPattern,
                    ctx.Symbol.GetLocation(),
                    kindDisplayName,
                    name,
                    "must",
                    "allow pattern",
                    allowPattern.ToString()));
            }
        }

        if (disallowPattern is not null)
        {
            if (TryIsMatch(disallowPattern, name))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NamingPattern,
                    ctx.Symbol.GetLocation(),
                    kindDisplayName,
                    name,
                    "must not",
                    "disallow pattern",
                    disallowPattern.ToString()));
            }
        }
    }

    private static void CheckNameForSymbol(SymbolAnalysisContext ctx, ISymbol symbol,
        string name, NamingTarget target, NamingPatternConfig config, string kindDisplayName)
    {
        if (string.IsNullOrEmpty(name))
            return;

        var (allowPattern, disallowPattern) = config.GetPatterns(target);

        if (allowPattern is not null)
        {
            if (!TryIsMatch(allowPattern, name))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NamingPattern,
                    symbol.GetLocation(),
                    kindDisplayName,
                    name,
                    "must",
                    "allow pattern",
                    allowPattern.ToString()));
            }
        }

        if (disallowPattern is not null)
        {
            if (TryIsMatch(disallowPattern, name))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NamingPattern,
                    symbol.GetLocation(),
                    kindDisplayName,
                    name,
                    "must not",
                    "disallow pattern",
                    disallowPattern.ToString()));
            }
        }
    }

    private static bool TryIsMatch(Regex pattern, string input)
    {
        try
        {
            return pattern.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static NamingTarget ClassifyMethod(IMethodSymbol method)
    {
        foreach (var attribute in method.Attributes)
        {
            if (attribute.AttributeKind == EnumProvider.AttributeKind.EventSubscriber)
                return NamingTarget.EventSubscriber;

            if (attribute.AttributeKind == EnumProvider.AttributeKind.IntegrationEvent ||
                attribute.AttributeKind == EnumProvider.AttributeKind.BusinessEvent)
                return NamingTarget.EventDeclaration;
        }

        if (method.IsLocal)
            return NamingTarget.LocalProcedure;

        return NamingTarget.GlobalProcedure;
    }

    private static string GetKindDisplayName(NamingTarget target) => target switch
    {
        NamingTarget.Procedure => "Procedure",
        NamingTarget.LocalProcedure => "Procedure",
        NamingTarget.GlobalProcedure => "Procedure",
        NamingTarget.EventSubscriber => "Event subscriber",
        NamingTarget.EventDeclaration => "Event declaration",
        _ => "Procedure"
    };

    private static string StripAffixes(string name, List<string>? affixes)
    {
        if (affixes is null || affixes.Count == 0)
            return name;

        foreach (var affix in affixes)
        {
            if (name.StartsWith(affix, StringComparison.OrdinalIgnoreCase) &&
                name.Length > affix.Length)
            {
                return name.Substring(affix.Length);
            }

            if (name.EndsWith(affix, StringComparison.OrdinalIgnoreCase) &&
                name.Length > affix.Length)
            {
                return name.Substring(0, name.Length - affix.Length);
            }
        }

        return name;
    }

    private static List<string>? GetAffixes(Compilation compilation)
    {
        AppSourceCopConfiguration? copConfiguration =
            AppSourceCopConfigurationProvider.GetAppSourceCopConfiguration(compilation);

        if (copConfiguration is null)
            return null;

        var affixes = new List<string>();
        if (!string.IsNullOrEmpty(copConfiguration.MandatoryPrefix) &&
            !affixes.Contains(copConfiguration.MandatoryPrefix, StringComparer.OrdinalIgnoreCase))
            affixes.Add(copConfiguration.MandatoryPrefix);

        if (copConfiguration.MandatoryAffixes is not null)
        {
            foreach (string mandatoryAffix in copConfiguration.MandatoryAffixes)
            {
                if (!string.IsNullOrEmpty(mandatoryAffix) &&
                    !affixes.Contains(mandatoryAffix, StringComparer.OrdinalIgnoreCase))
                    affixes.Add(mandatoryAffix);
            }
        }

        return affixes.Count > 0 ? affixes : null;
    }

    internal enum NamingTarget
    {
        Procedure,
        LocalProcedure,
        GlobalProcedure,
        EventSubscriber,
        EventDeclaration,
        Variable,
        Parameter,
        ReturnValue,
        Object,
        Field,
        Action,
        EnumValue,
        Control
    }

    internal sealed class NamingPatternConfig
    {
        private static readonly Dictionary<NamingTarget, (string? Allow, string? Disallow)> BuiltInDefaults = new()
        {
            [NamingTarget.Procedure] = (@"^[A-Z]", null),
            [NamingTarget.Variable] = (@"^[A-Z]", @"[%&!?]"),
            [NamingTarget.Parameter] = (@"^[A-Z]", null),
            [NamingTarget.ReturnValue] = (@"^[A-Z]", null),
            [NamingTarget.Object] = (@"^[A-Z]", null),
            [NamingTarget.Field] = (@"^[A-Za-z]", @"[%&!?]"),
            [NamingTarget.Action] = (@"^[A-Z]", null),
            [NamingTarget.EnumValue] = (@"^[A-Z]", null),
            [NamingTarget.Control] = (@"^[A-Z]", null),
        };

        private static readonly Dictionary<NamingTarget, NamingTarget> InheritanceMap = new()
        {
            [NamingTarget.LocalProcedure] = NamingTarget.Procedure,
            [NamingTarget.GlobalProcedure] = NamingTarget.Procedure,
            [NamingTarget.EventSubscriber] = NamingTarget.Procedure,
            [NamingTarget.EventDeclaration] = NamingTarget.Procedure,
        };

        private readonly Dictionary<NamingTarget, (Regex? Allow, Regex? Disallow)> _resolvedPatterns;

        public NamingPatternConfig(Dictionary<string, NamingPatternSetting>? userOverrides)
        {
            _resolvedPatterns = new Dictionary<NamingTarget, (Regex? Allow, Regex? Disallow)>();

            foreach (NamingTarget target in System.Enum.GetValues(typeof(NamingTarget)))
            {
                var (allowStr, disallowStr) = ResolvePatternStrings(target, userOverrides);
                _resolvedPatterns[target] = (
                    CompilePattern(allowStr),
                    CompilePattern(disallowStr));
            }
        }

        public (Regex? AllowPattern, Regex? DisallowPattern) GetPatterns(NamingTarget target) =>
            _resolvedPatterns.TryGetValue(target, out var patterns) ? patterns : (null, null);

        private static (string? Allow, string? Disallow) ResolvePatternStrings(
            NamingTarget target, Dictionary<string, NamingPatternSetting>? userOverrides)
        {
            // Check if user has explicit override for this target
            if (userOverrides is not null && TryGetUserOverride(userOverrides, target, out var userSetting))
            {
                return (
                    !string.IsNullOrEmpty(userSetting.AllowPattern) ? userSetting.AllowPattern : null,
                    !string.IsNullOrEmpty(userSetting.DisallowPattern) ? userSetting.DisallowPattern : null);
            }

            // Check if this target inherits from a parent
            if (InheritanceMap.TryGetValue(target, out var parent))
            {
                // Try user override for the parent
                if (userOverrides is not null && TryGetUserOverride(userOverrides, parent, out var parentSetting))
                {
                    return (
                        !string.IsNullOrEmpty(parentSetting.AllowPattern) ? parentSetting.AllowPattern : null,
                        !string.IsNullOrEmpty(parentSetting.DisallowPattern) ? parentSetting.DisallowPattern : null);
                }

                // Fall through to built-in default for parent
                if (BuiltInDefaults.TryGetValue(parent, out var parentDefault))
                    return parentDefault;
            }

            // Use built-in default for this target
            if (BuiltInDefaults.TryGetValue(target, out var builtIn))
                return builtIn;

            return (null, null);
        }

        private static bool TryGetUserOverride(
            Dictionary<string, NamingPatternSetting> overrides,
            NamingTarget target,
            out NamingPatternSetting setting)
        {
            var targetName = target.ToString();
            foreach (var kvp in overrides)
            {
                if (string.Equals(kvp.Key, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    setting = kvp.Value;
                    return true;
                }
            }

            setting = default!;
            return false;
        }

        private static Regex? CompilePattern(string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return null;

            try
            {
                return new Regex(
                    pattern.Trim(),
                    RegexOptions.Compiled | RegexOptions.CultureInvariant,
                    RegexTimeout);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
