page 50100 MyPage
{
    actions
    {
        area(Reporting)
        {
            action(RunSalesReport)
            {
                Caption = 'Run Sales Statistics';
                AboutTitle = 'About sales statistics';
                RunObject = report MySalesReport;
            }
        }
    }
}

report 50100 MySalesReport
{
    Caption = 'Sales Statistics';
    ProcessingOnly = true;

    requestpage
    {
        AboutTitle = 'About sales statistics';
        AboutText = 'Sales statistics summarize what you sold.';
    }
}
