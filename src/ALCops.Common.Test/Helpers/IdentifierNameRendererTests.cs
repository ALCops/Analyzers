using ALCops.Common.Helpers;

namespace ALCops.Common.Test;

public class IdentifierNameRendererTests
{
    private static readonly AcronymRegistry Registry = AcronymRegistry.Default;

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("/./")]
    public void Render_ReturnsEmpty_ForNullOrPunctuationOnly(string? input)
    {
        var result = IdentifierNameRenderer.Render(input, IdentifierCaseStyle.Pascal, Registry);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [TestCase("Sales Header", "SalesHeader")]
    [TestCase("Gen. Journal Template", "GenJournalTemplate")]
    [TestCase("G/L Account", "GLAccount")]
    [TestCase("Sales & Receivables Setup", "SalesReceivablesSetup")]
    [TestCase("OnAfterDeleteEvent", "OnAfterDeleteEvent")]
    public void Render_Pascal_SplitsAndCasesRegularWords(string input, string expected)
    {
        var result = IdentifierNameRenderer.Render(input, IdentifierCaseStyle.Pascal, Registry);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("Sales Header", "salesHeader")]
    [TestCase("OnAfterDeleteEvent", "onAfterDeleteEvent")]
    public void Render_Camel_LowercasesFirstWord(string input, string expected)
    {
        var result = IdentifierNameRenderer.Render(input, IdentifierCaseStyle.Camel, Registry);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(IdentifierCaseStyle.Snake, "Sales Header", "sales_header")]
    [TestCase(IdentifierCaseStyle.Snake, "LCY Amount", "lcy_amount")]
    [TestCase(IdentifierCaseStyle.Kebab, "Sales Header", "sales-header")]
    [TestCase(IdentifierCaseStyle.Kebab, "LCY Amount", "lcy-amount")]
    public void Render_SnakeAndKebab_LowercaseAll(IdentifierCaseStyle style, string input, string expected)
    {
        var result = IdentifierNameRenderer.Render(input, style, Registry);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Render_Pascal_PreservesKnownAcronymCanonicalCasing()
    {
        // Pick a mixed-case default entry (e.g. OData, UoM) so we can verify canonical
        // preservation from lower-case input.
        var mixedCase = AcronymRegistry.DefaultAcronyms
            .First(a => a.Any(char.IsLower) && a.Any(char.IsUpper));

        var result = IdentifierNameRenderer.Render(
            mixedCase.ToLowerInvariant() + " Setup",
            IdentifierCaseStyle.Pascal,
            Registry);

        Assert.That(result, Is.EqualTo(mixedCase + "Setup"));
    }

    [Test]
    public void Render_Pascal_KeepsAllUpperAcronymUpper()
    {
        // Default all-upper entry (e.g. LCY, HTTPS) stays upper regardless of source casing.
        var allUpper = AcronymRegistry.DefaultAcronyms
            .First(a => a.All(char.IsUpper) && a.Length >= 3);

        var result = IdentifierNameRenderer.Render(
            allUpper.ToLowerInvariant() + " Amount",
            IdentifierCaseStyle.Pascal,
            Registry);

        Assert.That(result, Is.EqualTo(allUpper + "Amount"));
    }

    [TestCase("Item ID", "ItemId")]
    [TestCase("Item id", "ItemId")]
    [TestCase("ID Card", "IdCard")]
    public void Render_Pascal_AlwaysNormalisesIdAbbreviation(string input, string expected)
    {
        var result = IdentifierNameRenderer.Render(input, IdentifierCaseStyle.Pascal, Registry);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("IO Log", "IOLog")]
    [TestCase("DX Setup", "DXSetup")]
    public void Render_Pascal_KeepsTwoLetterUppercaseWordsUpper(string input, string expected)
    {
        var result = IdentifierNameRenderer.Render(input, IdentifierCaseStyle.Pascal, Registry);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Render_Pascal_UserRegisteredAcronymOverridesCanonicalCasing()
    {
        // User pinning takes effect only when the source word carries no case signal
        // (all-lowercase). Original casing wins whenever any uppercase character is
        // present in the source, so "ACME Product" would keep "ACME" regardless of
        // any user registration. Passing an all-lowercase "acme product" makes the
        // registry the only source of truth and produces "AcmeProduct".
        var registry = AcronymRegistry.Create(new[] { "Acme" });

        var result = IdentifierNameRenderer.Render("acme product", IdentifierCaseStyle.Pascal, registry);

        Assert.That(result, Is.EqualTo("AcmeProduct"));
    }

    [Test]
    public void Render_Pascal_OriginalCasingWinsOverRegistry()
    {
        // Field "ACME Product" with user pinning "Acme" should still emit "ACMEProduct"
        // because the source word already carries an unambiguous casing signal.
        var registry = AcronymRegistry.Create(new[] { "Acme" });

        var result = IdentifierNameRenderer.Render("ACME Product", IdentifierCaseStyle.Pascal, registry);

        Assert.That(result, Is.EqualTo("ACMEProduct"));
    }

    [Test]
    public void Render_Pascal_OriginalMixedCasing_IsPreservedForRegisteredAcronym()
    {
        // Codeunit "Http Client Handler" -> word "Http" has uppercase 'H',
        // so the built-in registry entry "HTTP" must NOT override it.
        var result = IdentifierNameRenderer.Render(
            "Http Client Handler",
            IdentifierCaseStyle.Pascal,
            Registry);

        Assert.That(result, Is.EqualTo("HttpClientHandler"));
    }

    [TestCase("Line Discount %", "LineDiscount")]
    [TestCase("Tax %", "Tax")]
    [TestCase("% Complete", "Complete")]
    public void Render_Pascal_TreatsPercentAsWordDelimiter(string input, string expected)
    {
        var result = IdentifierNameRenderer.Render(input, IdentifierCaseStyle.Pascal, Registry);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("Sales Header")]
    [TestCase("Line Discount %")]
    [TestCase("G/L Account")]
    [TestCase("OnAfterInsertEvent")]
    [TestCase("Amount Incl. VAT")]
    public void Render_Raw_EmitsInputVerbatim(string input)
    {
        var result = IdentifierNameRenderer.Render(input, IdentifierCaseStyle.Raw, Registry);

        Assert.That(result, Is.EqualTo(input));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Render_Raw_ReturnsEmpty_ForNullOrEmpty(string? input)
    {
        var result = IdentifierNameRenderer.Render(input, IdentifierCaseStyle.Raw, Registry);

        Assert.That(result, Is.EqualTo(string.Empty));
    }
}
