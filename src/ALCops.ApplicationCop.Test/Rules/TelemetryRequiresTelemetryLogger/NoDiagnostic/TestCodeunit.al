codeunit 50100 [|MyCodeunit|]
{
    Subtype = Test;

    trigger OnRun()
    var
        Telemetry: Codeunit Telemetry;
    begin
    end;
}

codeunit 8711 Telemetry
{
}

codeunit 8703 "Feature Telemetry"
{
}

interface "Telemetry Logger"
{
    procedure LogMessage(EventId: Text; Message: Text; Verbosity: Verbosity; DataClassification: DataClassification; TelemetryScope: TelemetryScope; CustomDimensions: Dictionary of [Text, Text]);
}
