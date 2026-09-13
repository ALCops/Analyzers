codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyRecordRef: RecordRef;
    begin
        [|MyRecordRef.LockTable();|]
    end;
}
