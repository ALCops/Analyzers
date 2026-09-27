report 50000 MyReport
{
    ApplicationArea = All;
    UsageCategory = ReportsAndAnalysis;
    ProcessingOnly = true;
    AccessByPermission = tabledata MyTable = R;

    dataset
    {
        dataitem(MyTable; MyTable)
        {
            trigger OnAfterGetRecord()
            begin
                [|MyTable.Modify();|]
            end;
        }
    }
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
        field(2; MyField2; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
