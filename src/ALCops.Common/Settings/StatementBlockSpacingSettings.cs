namespace ALCops.Common.Settings;

public sealed class StatementBlockSpacingSettings
{
    public bool ControlFlowBefore { get; set; } = true;
    public bool ControlFlowAfter { get; set; } = true;
    public ScopeLeavingMode ScopeLeavingMode { get; set; } = ScopeLeavingMode.ExitAndError;
    public ElseChainBeforeMode ElseChainBeforeMode { get; set; } = ElseChainBeforeMode.Off;
    public OneLinerMode OneLinerMode { get; set; } = OneLinerMode.None;
}

public enum ScopeLeavingMode
{
    Off,
    ExitOnly,
    ErrorOnly,
    ExitAndError,
}

public enum ElseChainBeforeMode
{
    Off,
    RequireBlank,
}

public enum OneLinerMode
{
    None,
    All,
}
