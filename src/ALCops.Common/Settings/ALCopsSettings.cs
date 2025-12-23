namespace ALCops.Common.Settings;

public sealed class ALCopsSettings
{
    public int CognitiveComplexityThreshold { get; set; } = 15;
    public int CyclomaticComplexityThreshold { get; set; } = 8;
    public int MaintainabilityIndexThreshold { get; set; } = 20;
}
