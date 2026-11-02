codeunit 50100 TryFunctionSafeMethods
{
    [TryFunction]
    local procedure TryWork()
    var
        MyRecord: Record MyTable;
    begin
        [|MyRecord.FindFirst()|];
        [|MyRecord.Get(1)|];
        [|Message('No database write')|];
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
