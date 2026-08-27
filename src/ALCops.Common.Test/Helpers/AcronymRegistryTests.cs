using ALCops.Common.Helpers;

namespace ALCops.Common.Test;

public class AcronymRegistryTests
{
    [Test]
    public void Default_ExposesEveryEntryFromDefaultAcronyms()
    {
        Assume.That(AcronymRegistry.DefaultAcronyms.Count, Is.GreaterThan(0));

        foreach (var acronym in AcronymRegistry.DefaultAcronyms)
        {
            Assert.That(
                AcronymRegistry.Default.TryGetVariants(acronym, out var variants),
                Is.True,
                $"Default should expose '{acronym}' as a registered variant");

            Assert.That(
                variants,
                Does.Contain(acronym),
                $"'{acronym}' must be present among registered variants for its case-insensitive key");
        }
    }

    [Test]
    public void TryGetCanonical_IsCaseInsensitive()
    {
        // Pick the first mixed-case default entry (e.g. "OData", "UoM") so we can verify
        // that all-upper and all-lower lookups map back to the canonical stored casing.
        var mixedCase = AcronymRegistry.DefaultAcronyms
            .FirstOrDefault(a => a.Any(char.IsLower) && a.Any(char.IsUpper));

        Assume.That(mixedCase, Is.Not.Null, "Default list should contain at least one mixed-case acronym");

        Assert.That(AcronymRegistry.Default.TryGetCanonical(mixedCase!.ToUpperInvariant(), out var fromUpper), Is.True);
        Assert.That(fromUpper, Is.EqualTo(mixedCase));

        Assert.That(AcronymRegistry.Default.TryGetCanonical(mixedCase.ToLowerInvariant(), out var fromLower), Is.True);
        Assert.That(fromLower, Is.EqualTo(mixedCase));
    }

    [Test]
    public void TryGetCanonical_ReturnsFalseForUnknownWord()
    {
        // Use a word guaranteed not to be a default entry.
        const string unknown = "ThisIsNotARegisteredAcronym";

        Assume.That(
            AcronymRegistry.DefaultAcronyms,
            Has.None.EqualTo(unknown).IgnoreCase);

        Assert.That(AcronymRegistry.Default.TryGetCanonical(unknown, out var value), Is.False);
        Assert.That(value, Is.EqualTo(string.Empty));
    }

    [Test]
    public void TryGetCanonical_ReturnsFalseForEmptyOrNull()
    {
        Assert.That(AcronymRegistry.Default.TryGetCanonical(string.Empty, out _), Is.False);
        Assert.That(AcronymRegistry.Default.TryGetCanonical(null!, out _), Is.False);
    }

    [Test]
    public void Create_MergesUserAcronymsWithDefaults()
    {
        var firstDefault = AcronymRegistry.DefaultAcronyms[0];
        const string userEntry = "MyCoDomainAcronym";

        var registry = AcronymRegistry.Create(new[] { userEntry });

        Assert.That(registry.TryGetCanonical(firstDefault, out var builtIn), Is.True);
        Assert.That(builtIn, Is.EqualTo(firstDefault));

        Assert.That(registry.TryGetCanonical(userEntry, out var custom), Is.True);
        Assert.That(custom, Is.EqualTo(userEntry));
    }

    [Test]
    public void Create_UserEntryOverridesBuiltInCanonicalCasing()
    {
        // Pick the first default entry and override its casing with a lower-first variant.
        var firstDefault = AcronymRegistry.DefaultAcronyms[0];
        var overriddenCasing = firstDefault.ToLowerInvariant();

        Assume.That(overriddenCasing, Is.Not.EqualTo(firstDefault));

        var registry = AcronymRegistry.Create(new[] { overriddenCasing });

        Assert.That(registry.TryGetCanonical(firstDefault, out var canonical), Is.True);
        Assert.That(canonical, Is.EqualTo(overriddenCasing));
    }

    [Test]
    public void Create_IgnoresNullOrWhitespaceEntries()
    {
        const string userEntry = "MyCoDomainAcronym";
        var registry = AcronymRegistry.Create(new[] { userEntry, "", "   ", null! });

        Assert.That(registry.TryGetCanonical(userEntry, out var canonical), Is.True);
        Assert.That(canonical, Is.EqualTo(userEntry));
    }

    [Test]
    public void Create_TrimsUserEntries()
    {
        const string userEntry = "MyCoDomainAcronym";
        var registry = AcronymRegistry.Create(new[] { $"  {userEntry}  " });

        Assert.That(registry.TryGetCanonical(userEntry, out var canonical), Is.True);
        Assert.That(canonical, Is.EqualTo(userEntry));
    }

    [Test]
    public void Create_WithNullUserList_ReturnsDefaultsOnly()
    {
        var registry = AcronymRegistry.Create(userAcronyms: null);

        foreach (var acronym in AcronymRegistry.DefaultAcronyms)
        {
            Assert.That(registry.TryGetVariants(acronym, out var variants), Is.True);
            Assert.That(variants, Does.Contain(acronym));
        }

        Assert.That(registry.TryGetCanonical("MyCoDomainAcronym", out _), Is.False);
    }

    [Test]
    public void TryGetVariants_ExposesAllRegisteredVariantsForSameKey()
    {
        // Defaults list both "BoM" (canonical) and "Bom" for the "BOM" upper-key.
        Assert.That(AcronymRegistry.Default.TryGetVariants("bom", out var variants), Is.True);
        Assert.That(variants, Is.EquivalentTo(new[] { "BoM", "Bom" }));
    }

    [Test]
    public void TryGetCanonical_ReturnsFirstAddedVariantForKey()
    {
        // Defaults order for the "BOM" upper-key is ["BoM", "Bom"] -> canonical is "BoM".
        Assert.That(AcronymRegistry.Default.TryGetCanonical("BOM", out var canonical), Is.True);
        Assert.That(canonical, Is.EqualTo("BoM"));
    }

    [Test]
    public void Create_UserEntriesDisplaceDefaultVariantsForSameKey()
    {
        // Defaults contain "BoM" and "Bom" under the "BOM" key. Supplying "bOm" as user
        // entry must wipe those defaults and become the sole variant for that key.
        var registry = AcronymRegistry.Create(new[] { "bOm" });

        Assert.That(registry.TryGetVariants("BOM", out var variants), Is.True);
        Assert.That(variants, Is.EqualTo(new[] { "bOm" }));
    }

    [Test]
    public void Create_MultipleUserEntriesForSameKey_AllRegisteredInOrder()
    {
        var registry = AcronymRegistry.Create(new[] { "Lcy", "LCY" });

        Assert.That(registry.TryGetVariants("lcy", out var variants), Is.True);
        Assert.That(variants, Is.EqualTo(new[] { "Lcy", "LCY" }));

        Assert.That(registry.TryGetCanonical("lcy", out var canonical), Is.True);
        Assert.That(canonical, Is.EqualTo("Lcy"));
    }
}
