codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        TempMyTable: Record MyTable temporary;
        MyPage: Page MyPage;
    begin
        [|MyPage.SetRecord(TempMyTable)|];
    end;
}

page 50100 MyPage
{
    SourceTable = MyTable;

    trigger OnFindRecord(Which: Text): Boolean
    begin
        TempBuffer.Copy(Rec);
        if not TempBuffer.Find(Which) then
            exit(false);
        Rec := TempBuffer;
        exit(true);
    end;

    var
        TempBuffer: Record MyTable temporary;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
