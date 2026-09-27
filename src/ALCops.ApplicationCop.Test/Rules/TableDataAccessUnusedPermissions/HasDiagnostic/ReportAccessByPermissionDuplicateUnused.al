report 50000 MyReport
{
    ApplicationArea = All;
    UsageCategory = ReportsAndAnalysis;
    ProcessingOnly = true;
    AccessByPermission = tabledata MyTable = R;
    Permissions = [|tabledata MyTable = r|];

    trigger OnPreReport()
    begin
    end;
}

table 50000 MyTable
{
    Caption = '', Locked = true;

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
