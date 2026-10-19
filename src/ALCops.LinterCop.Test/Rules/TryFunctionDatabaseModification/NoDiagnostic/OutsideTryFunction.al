codeunit 50100 TryFunctionOutside
{
    local procedure NormalProcedure()
    var
        MyRecord: Record MyTable;
    begin
        [|MyRecord.Insert()|];
        [|MyRecord.Modify()|];
        [|MyRecord.DeleteAll()|];
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
