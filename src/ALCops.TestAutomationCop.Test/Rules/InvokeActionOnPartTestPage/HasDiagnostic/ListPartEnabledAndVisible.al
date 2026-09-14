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
    procedure MyTest()
    var
        SubPage: TestPage MyListPart;
        IsEnabled: Boolean;
        IsVisible: Boolean;
    begin
        SubPage.OpenView();
        IsEnabled := [|SubPage.MyAction.Enabled()|];
        IsVisible := [|SubPage.MyAction.Visible()|];
    end;
}
