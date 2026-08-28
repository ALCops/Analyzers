using ALCops.Common.Permissions;

namespace ALCops.Common.Test;

public class NaturalStringComparerTests
{
    private static int Compare(string? x, string? y) => NaturalStringComparer.Instance.Compare(x, y);

    [TestCase("Item 2", "Item 10")]
    [TestCase("Item 2", "Item 100")]
    [TestCase("Item 1", "Item 2")]
    [TestCase("A 1", "A 99999999999999999999")]
    [TestCase("1A", "A1")]
    public void Compare_OrdersNaturally(string smaller, string larger)
    {
        Assert.That(Compare(smaller, larger), Is.LessThan(0));
        Assert.That(Compare(larger, smaller), Is.GreaterThan(0));
    }

    [Test]
    public void Compare_LeadingZerosAreIgnored_ThenShorterStringWins()
    {
        // AZ parses both chunks to 10, then falls back to the original length difference.
        Assert.That(Compare("Item 10", "Item 010"), Is.LessThan(0));
        Assert.That(Compare("Item 010", "Item 10"), Is.GreaterThan(0));
    }

    [Test]
    public void Compare_IssueReproPair_DotBeforeLetterAfterSpacesAreStripped()
    {
        // #245: "Post.Appr.SetupWizHlp" < "PostInv.Appr.SetupHlp" because '.' sorts before 'I'.
        Assert.That(Compare("Post. Appr. Setup Wiz Hlp", "Post Inv. Appr. Setup Hlp"), Is.LessThan(0));
    }

    [Test]
    public void Compare_SpacesAreIgnored()
    {
        // Ordinal comparison would put "A B" first (space < 'A'); AZ compares "AB" vs "AA".
        Assert.That(Compare("AA", "A B"), Is.LessThan(0));
    }

    [TestCase("alpha", "ALPHA")]
    [TestCase("Item 10", "item 10")]
    public void Compare_IsCaseInsensitive(string x, string y)
    {
        Assert.That(Compare(x, y), Is.EqualTo(0));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Compare_EmptyValuesSortLast(string? empty)
    {
        Assert.That(Compare(empty, "A"), Is.GreaterThan(0));
        Assert.That(Compare("A", empty), Is.LessThan(0));
        Assert.That(Compare(empty, " "), Is.EqualTo(0));
    }
}
