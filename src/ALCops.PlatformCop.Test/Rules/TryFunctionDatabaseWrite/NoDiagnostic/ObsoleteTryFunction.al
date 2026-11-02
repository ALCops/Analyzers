codeunit 50100 ObsoleteTryFunction
{
    [TryFunction]
    [Obsolete]
    local procedure TryWork()
    var
        MyRecord: Record MyTable;
    begin
        [|MyRecord.Insert()|];
        [|MyCustomProcedure()|];
    end;

    local procedure MyCustomProcedure()
    var
        MyRecord: Record MyTable;
    begin
        MyRecord.Insert();
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
