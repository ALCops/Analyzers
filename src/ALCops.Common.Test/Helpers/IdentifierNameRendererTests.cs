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
        // Use an explicit registry entry to verify that an all-uppercase acronym
        // can still be preserved as canonical output from lower-case input.
        const string allUpper = "LCY";
        var registry = AcronymRegistry.Create(new[] { allUpper });

        var result = IdentifierNameRenderer.Render(
            allUpper.ToLowerInvariant() + " Amount",
            IdentifierCaseStyle.Pascal,
            registry);

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

    [Test]
    public void RenderAccepted_Pascal_ReturnsSingleElement_WhenNoAcronymCollision()
    {
        var result = IdentifierNameRenderer.RenderAccepted(
            "Sales Header",
            IdentifierCaseStyle.Pascal,
            Registry);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("SalesHeader"));
    }

    [Test]
    public void RenderAccepted_Pascal_AddsRegistryVariant_WhenUserPinsAlternateCasingForUppercaseWord()
    {
        // User pins "Lcy"; source event "OnAfterCalcOverdueBalanceLCY" ends in uppercase
        // "LCY". Both spellings must be accepted; the preferred stays "LCY" (original wins).
        var registry = AcronymRegistry.Create(new[] { "Lcy" });

        var result = IdentifierNameRenderer.RenderAccepted(
            "OnAfterCalcOverdueBalanceLCY",
            IdentifierCaseStyle.Pascal,
            registry);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo("OnAfterCalcOverdueBalanceLCY"));
        Assert.That(result[1], Is.EqualTo("OnAfterCalcOverdueBalanceLcy"));
    }

    [Test]
    public void RenderAccepted_Pascal_CrossProduct_WhenTwoWordsHaveRegistryAlternates()
    {
        // Two ambiguous words -> 2x2 = 4 accepted variants. Preferred (original) is [0].
        var registry = AcronymRegistry.Create(new[] { "Lcy", "Vat" });

        var result = IdentifierNameRenderer.RenderAccepted(
            "LCY VAT Amount",
            IdentifierCaseStyle.Pascal,
            registry);

        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result[0], Is.EqualTo("LCYVATAmount"));
        Assert.That(result, Does.Contain("LCYVatAmount"));
        Assert.That(result, Does.Contain("LcyVATAmount"));
        Assert.That(result, Does.Contain("LcyVatAmount"));
    }

    [Test]
    public void RenderAccepted_Pascal_NoAlternate_WhenRegistryCasingEqualsOriginal()
    {
        // Source "Vat Amount" + default registry entry "Vat" -> registry casing equals
        // the original-wins primary, so no second variant is emitted.
        var result = IdentifierNameRenderer.RenderAccepted(
            "Vat Amount",
            IdentifierCaseStyle.Pascal,
            Registry);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("VatAmount"));
    }

    [Test]
    public void RenderAccepted_Pascal_AllLowercaseSource_ReturnsSingleRegistryCanonical()
    {
        // All-lowercase source has no case signal to preserve, so only the registry
        // canonical is produced (no cross product).
        var result = IdentifierNameRenderer.RenderAccepted(
            "vat amount",
            IdentifierCaseStyle.Pascal,
            Registry);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("VatAmount"));
    }

    [Test]
    public void RenderAccepted_Snake_NeverProducesAlternates()
    {
        var registry = AcronymRegistry.Create(new[] { "Lcy" });

        var result = IdentifierNameRenderer.RenderAccepted(
            "OnAfterCalcOverdueBalanceLCY",
            IdentifierCaseStyle.Snake,
            registry);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("on_after_calc_overdue_balance_lcy"));
    }

    [Test]
    public void RenderAccepted_Camel_FirstWordAlwaysLowercased_NoAlternate()
    {
        // Even if the first word matches a registered acronym alternate, camelCase
        // forces the first word to lowercase and does not produce alternates for it.
        var registry = AcronymRegistry.Create(new[] { "Lcy" });

        var result = IdentifierNameRenderer.RenderAccepted(
            "LCY Amount",
            IdentifierCaseStyle.Camel,
            registry);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("lcyAmount"));
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
