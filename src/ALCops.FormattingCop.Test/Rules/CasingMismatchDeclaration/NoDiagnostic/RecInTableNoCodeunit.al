codeunit 50108 MyCodeunit
{
    TableNo = MyTable;

    trigger OnRun()
    begin
        [|Rec|].TestField("No.");
        [|Rec|]."No." := 'X';
    end;
}

table 50108 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }
}
