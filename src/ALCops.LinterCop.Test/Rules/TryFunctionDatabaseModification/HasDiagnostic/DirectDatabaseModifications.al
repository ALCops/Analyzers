codeunit 50100 TryFuncDbModification
{
    [TryFunction]
    local procedure InsertRecord()
    var
        MyRecord: Record MyTable;
    begin
        [|MyRecord.Insert()|];
        [|MyRecord.Modify()|];
        [|MyRecord.ModifyAll(MyField, 1)|];
        [|MyRecord.Delete()|];
        [|MyRecord.DeleteAll()|];
        [|MyRecord.Truncate()|];
        [|MyRecord.Rename(2)|];
        [|MyRecord.AddLink('https://example.com')|];
        [|MyRecord.CopyLinks(MyRecord)|];
        [|MyRecord.DeleteLink(1)|];
        [|MyRecord.DeleteLinks()|];
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
