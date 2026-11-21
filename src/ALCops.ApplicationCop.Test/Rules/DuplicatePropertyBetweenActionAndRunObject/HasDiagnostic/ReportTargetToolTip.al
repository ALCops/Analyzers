page 50100 MyPage
{
    actions
    {
        area(Reporting)
        {
            action(RunSalesReport)
            {
                Caption = 'Run Sales Statistics';
                [|ToolTip = 'Print the sales statistics.';|]
                RunObject = report MySalesReport;
            }
        }
    }
}

report 50100 MySalesReport
{
    Caption = 'Sales Statistics';
    ToolTip = 'Print the sales statistics.';
    ProcessingOnly = true;
}
