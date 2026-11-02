page 50100 MyPage
{
    SourceTable = MyTable;
    SourceTableTemporary = true;

    [TryFunction]
    local procedure TryWork()
    begin
        [|Rec.Insert()|];
    end;
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
