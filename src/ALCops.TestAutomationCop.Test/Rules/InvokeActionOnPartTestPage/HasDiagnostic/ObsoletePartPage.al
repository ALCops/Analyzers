table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

page 50100 MyListPart
{
    ObsoleteState = Pending;
    ObsoleteReason = 'Replaced by a new page.';
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
    procedure MyTest()
    var
        SubPage: TestPage MyListPart;
    begin
        SubPage.OpenView();
        [|SubPage.MyAction.Invoke()|];
    end;
}
