codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTempTable: Record MyTempTable;
        MyPage: Page MyPage;
    begin
        [|MyPage.SetRecord(MyTempTable)|];
    end;
}

page 50100 MyPage { SourceTable = MyTempTable; }
table 50100 MyTempTable
{
    TableType = Temporary;

    fields
    {
        field(1; MyField; Integer) { }
    }
}
