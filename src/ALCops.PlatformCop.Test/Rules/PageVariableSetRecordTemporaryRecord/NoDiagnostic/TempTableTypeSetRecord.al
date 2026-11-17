codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        TempMyTable: Record MyTempTable temporary;
        MyPage: Page MyPage;
    begin
        [|MyPage.SetRecord(TempMyTable)|];
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
