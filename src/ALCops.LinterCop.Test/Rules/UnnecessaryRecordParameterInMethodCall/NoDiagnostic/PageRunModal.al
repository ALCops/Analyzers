table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

page 50100 MyPage
{
    PageType = Card;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            group(General)
            {
                field(Name; Rec.Name) { }
            }
        }
    }
}

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        [|Page.RunModal(Page::MyPage, MyTable)|];
    end;
}
