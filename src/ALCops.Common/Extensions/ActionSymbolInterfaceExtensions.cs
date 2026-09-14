using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace ALCops.Common.Extensions;

public static class ActionSymbolInterfaceExtensions
{
    /// <summary>
    /// Tells whether the action is a group whose name binds it to one of the platform's predefined promoted
    /// category slots (<c>Category_New</c>, <c>Category_Process</c>, <c>Category_Report</c>,
    /// <c>Category_Category4</c> .. <c>Category_Category20</c>). Such a group takes its name and its caption
    /// from the platform, so it is neither renameable nor missing a caption of its own.
    /// </summary>
    /// <remarks>
    /// <c>SyntaxFacts.PromotedCategoriesSynthesizedSymbolNames</c> is the SDK's own immutable set, built over
    /// every <c>PromotedCategoryKind</c> member with an ordinal case-insensitive comparer, which matches AL's
    /// case-insensitive identifiers. It holds the same names as <c>SyntaxFacts.PredefinedActionCategoryNames</c>
    /// and needs no local copy or lowercasing at the call site.
    /// </remarks>
    public static bool IsPredefinedPromotedCategoryGroup(this IActionSymbol action) =>
        action.ActionKind == EnumProvider.ActionKind.Group &&
        SyntaxFacts.PromotedCategoriesSynthesizedSymbolNames.Contains(action.Name);
}
