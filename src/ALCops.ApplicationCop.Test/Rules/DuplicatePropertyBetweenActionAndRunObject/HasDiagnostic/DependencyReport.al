page 50100 MyPage
{
    actions
    {
        area(Reporting)
        {
            action(RunSalesReport)
            {
                [|Caption = 'Sales Statistics';|]
                [|ToolTip = 'Print the sales statistics.';|]
                RunObject = report MySalesReport;
            }
        }
    }
}
