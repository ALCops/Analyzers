codeunit 50100 [|MyFirstCodeunit|]
{
    var
        Telemetry: Codeunit Telemetry;
}

codeunit 50101 [|MySecondCodeunit|]
{
    procedure DoSomething()
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
