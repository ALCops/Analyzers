namespace ALCops.Common.Settings;

public sealed class ALCopsSettings
{
    public int CognitiveComplexityThreshold { get; set; } = 15;
    public int CyclomaticComplexityThreshold { get; set; } = 8;
    public int MaintainabilityIndexThreshold { get; set; } = 20;
    public string[]? LanguagesToTranslate { get; set; }
    public Dictionary<string, NamingPattern>? NamingPatterns { get; set; }
    public string? UseSequentialGuidScope { get; set; }
}
