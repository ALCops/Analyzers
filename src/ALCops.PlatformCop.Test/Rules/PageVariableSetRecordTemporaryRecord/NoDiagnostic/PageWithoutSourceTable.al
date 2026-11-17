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
    PageType = Card;

    layout
    {
        area(Content)
        {
            field(MyField; MyField) { ApplicationArea = All; }
        }
    }

    var
        MyField: Integer;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
