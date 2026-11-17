codeunit 50108 MyCodeunit
{
    TableNo = MyTable;

    trigger OnRun()
    begin
        [|rec|].TestField("No.");
        [|REC|]."No." := 'X';
    end;
}

table 50108 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }
}
