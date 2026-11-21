page 50100 MyPage
{
    actions
    {
        area(Reporting)
        {
            action(RunSalesReport)
            {
                Caption = 'Run Sales Statistics';
                [|AboutTitle = 'About sales statistics';|]
                [|AboutText = 'Sales statistics summarize what you sold.';|]
                RunObject = report MySalesReport;
            }
        }
    }
}
