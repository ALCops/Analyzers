page 50100 MyPage
{
    SourceTable = MyTable;

    trigger OnOpenPage()
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
