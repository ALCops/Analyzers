table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

page 50100 MyListPage
{
    PageType = List;
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
        MyPage: TestPage MyListPage;
    begin
        MyPage.OpenView();
        [|MyPage.MyAction.Invoke()|];
    end;
}
