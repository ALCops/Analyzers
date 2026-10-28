codeunit 50100 TryFuncPotentialMods
{
    [TryFunction]
    local procedure TryWork()
    var
        MyRecord: Record MyTable;
    begin
        [|Commit()|];
        [|MyRecord.Validate(MyField, 1)|];
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
