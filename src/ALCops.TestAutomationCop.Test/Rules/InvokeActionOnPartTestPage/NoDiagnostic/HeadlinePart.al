page 50100 MyHeadlinePart
{
    PageType = HeadlinePart;

    layout
    {
        area(content)
        {
            group(Control1)
            {
                ShowCaption = false;

                field(Headline; 'Hello')
                {
                }
            }
        }
    }

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
        MyPage: TestPage MyHeadlinePart;
    begin
        MyPage.OpenView();
        [|MyPage.MyAction.Invoke()|];
    end;
}
