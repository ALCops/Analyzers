codeunit 50100 [|MyCodeunit|]
{
    procedure DoSomething()
    begin
        Session.LogMessage('0000ABC', 'Message', Verbosity::Normal, DataClassification::SystemMetadata, TelemetryScope::ExtensionPublisher, 'Category', 'Test');
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
