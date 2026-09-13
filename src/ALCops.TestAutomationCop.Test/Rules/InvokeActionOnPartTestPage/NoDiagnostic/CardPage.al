table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

page 50100 MyCardPage
{
    PageType = Card;
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

codeunit 50100 MyTestCodeunit
{
    Subtype = Test;

    [Test]
    procedure MyTest()
    var
        MyPage: TestPage MyCardPage;
    begin
        MyPage.OpenView();
        [|MyPage.MyAction.Invoke()|];
    end;
}
