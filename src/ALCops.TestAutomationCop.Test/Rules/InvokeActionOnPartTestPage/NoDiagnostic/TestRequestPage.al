report 50100 MyReport
{
    requestpage
    {
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
}

codeunit 50100 MyTestCodeunit
{
    Subtype = Test;

    [Test]
    [HandlerFunctions('MyReportRequestPageHandler')]
    procedure MyTest()
    begin
        Report.Run(Report::MyReport);
    end;

    [RequestPageHandler]
    procedure MyReportRequestPageHandler(var RequestPage: TestRequestPage MyReport)
    begin
        [|RequestPage.MyAction.Invoke()|];
    end;
}
