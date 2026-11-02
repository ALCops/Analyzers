codeunit 50100 TryFunctionTemporaryRecord
{
    [TryFunction]
    local procedure TryWork()
    var
        TempMyRecord: Record MyTable temporary;
        MyTempTable: Record MyTempTable;
    begin
        [|TempMyRecord.Insert()|];
        [|TempMyRecord.DeleteAll()|];
        [|MyTempTable.Insert()|];
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

table 50101 MyTempTable
{
    TableType = Temporary;

    fields
    {
        field(1; MyField; Integer)
        {
        }
    }
}
