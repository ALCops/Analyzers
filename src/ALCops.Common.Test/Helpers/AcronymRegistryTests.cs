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
                AcronymRegistry.Default.TryGetCanonical(acronym, out var canonical),
                Is.True,
                $"Default should expose '{acronym}'");

            Assert.That(
                canonical,
                Is.EqualTo(acronym),
                $"Canonical casing for '{acronym}' must be preserved");
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
        var firstDefault = AcronymRegistry.DefaultAcronyms.First();
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
        var firstDefault = AcronymRegistry.DefaultAcronyms.First();
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
            Assert.That(registry.TryGetCanonical(acronym, out var canonical), Is.True);
            Assert.That(canonical, Is.EqualTo(acronym));
        }

        Assert.That(registry.TryGetCanonical("MyCoDomainAcronym", out _), Is.False);
    }
}
