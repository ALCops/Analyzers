pageextension 50100 MyPageExt extends MyPage
{
    trigger OnOpenPage()
    var
        TempMyTable: Record MyTable temporary;
    begin
        [|CurrPage.SetRecord(TempMyTable)|];
    end;
}

page 50100 MyPage { SourceTable = MyTable; }
table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
