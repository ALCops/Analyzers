page 50100 [|MyPage|]
{
    SourceTable = MyTable;

    actions
    {
        area(Processing)
        {
            action(MyAction)
            {
                trigger OnAction()
                var
                    FeatureTelemetry: Codeunit "Feature Telemetry";
                begin
                end;
            }
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer)
        {
        }
    }
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
