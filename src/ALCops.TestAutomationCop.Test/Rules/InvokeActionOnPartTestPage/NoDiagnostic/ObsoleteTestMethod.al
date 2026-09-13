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

codeunit 50100 MyTestCodeunit
{
    Subtype = Test;

    [Test]
    [Obsolete('Replaced by a new test.', '1.0.0.0')]
    procedure MyTest()
    var
        SubPage: TestPage MyListPart;
    begin
        SubPage.OpenView();
        [|SubPage.MyAction.Invoke()|];
    end;
}
