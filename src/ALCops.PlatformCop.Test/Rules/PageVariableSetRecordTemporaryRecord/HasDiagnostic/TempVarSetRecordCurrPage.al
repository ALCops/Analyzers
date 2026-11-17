page 50100 MyPage
{
    SourceTable = MyTable;

    trigger OnOpenPage()
    var
        TempMyTable: Record MyTable temporary;
    begin
        [|CurrPage.SetRecord(TempMyTable)|];
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
