pageextension 50100 MyPageExt extends MyPage
{
    trigger OnOpenPage()
    var
        TempMyTable: Record MyTable temporary;
    begin
        [|CurrPage.SetRecord(TempMyTable)|];
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
