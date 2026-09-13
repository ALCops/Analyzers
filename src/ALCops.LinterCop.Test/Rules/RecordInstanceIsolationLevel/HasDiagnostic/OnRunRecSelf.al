codeunit 50100 MyCodeunit
{
    TableNo = MyTable;

    trigger OnRun()
    begin
        [|Rec.LockTable();|]
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
