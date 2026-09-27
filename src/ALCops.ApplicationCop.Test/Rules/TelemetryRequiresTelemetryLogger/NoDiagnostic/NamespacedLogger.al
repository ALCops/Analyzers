namespace MyPublisher.MyExtension.MyAppDomain;

codeunit 50100 [|MyCodeunit|]
{
    var
        Telemetry: Codeunit MyPublisher.MyExtension.MyAppDomain.Telemetry;
}

codeunit 50101 MyTelemetryLogger implements MyPublisher.MyExtension.MyAppDomain."Telemetry Logger"
{
    procedure LogMessage(EventId: Text; Message: Text; Verbosity: Verbosity; DataClassification: DataClassification; TelemetryScope: TelemetryScope; CustomDimensions: Dictionary of [Text, Text])
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
