page 50100 MyPage
{
    actions
    {
        area(Reporting)
        {
            action(RunSalesReport)
            {
                [|Caption = 'Sales Statistics';|]
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
