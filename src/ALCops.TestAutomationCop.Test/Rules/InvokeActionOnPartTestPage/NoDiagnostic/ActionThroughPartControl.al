table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

page 50100 MyListPart
{
    PageType = ListPart;
    SourceTable = MyTable;

    actions
    {
        area(processing)
        {
            action(MyAction)
            {
            }
        }
    }
}

page 50101 MyHostPage
{
    PageType = Card;
    SourceTable = MyTable;

    layout
    {
        area(content)
        {
            part(SubPagePart; MyListPart) { }
        }
    }
}

codeunit 50100 MyTestCodeunit
{
    Subtype = Test;

    [Test]
    procedure MyTest()
    var
        MainPage: TestPage MyHostPage;
    begin
        MainPage.OpenView();
        [|MainPage.SubPagePart.MyAction.Invoke()|];
    end;
}
