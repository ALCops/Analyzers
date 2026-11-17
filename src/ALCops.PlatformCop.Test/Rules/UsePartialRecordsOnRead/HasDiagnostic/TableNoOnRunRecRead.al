codeunit 50100 MyCodeunit
{
    TableNo = MyTable;

    trigger OnRun()
    begin
        [|Rec.FindFirst()|];
        Message(Rec.MyField);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        field(2; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Entry No.") { }
    }
}
