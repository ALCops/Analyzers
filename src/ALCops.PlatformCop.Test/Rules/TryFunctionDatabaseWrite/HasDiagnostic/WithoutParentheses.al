codeunit 50100 TryFunctionWithoutParentheses
{
    [TryFunction]
    local procedure TryWork()
    var
        MyRecord: Record MyTable;
    begin
        [|MyRecord.Insert|];
        [|MyRecord.DeleteAll|];
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
