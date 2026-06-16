codeunit 50000 MyCodeunit
{

    trigger OnRun()
    var
        MyTable: Record "ABC Example Header.Line";
    begin
        [|MyTable.FindFirst();|]
    end;
}

table 50000 "ABC Example Header.Line"
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
