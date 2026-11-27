table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }

    procedure DoSomething(var R: RecordRef)
    begin
    end;

    procedure CallWithRecordRef()
    var
        RecRef: RecordRef;
    begin
        RecRef.GetTable(Rec);
        DoSomething([|RecRef|]);
    end;
}

codeunit 50100 MyCodeunit
{
    procedure CallMyProcedure()
    var
        MyTable: Record MyTable;
        RecRef: RecordRef;
    begin
        RecRef.GetTable(MyTable);
        MyTable.DoSomething([|RecRef|]);
    end;
}
