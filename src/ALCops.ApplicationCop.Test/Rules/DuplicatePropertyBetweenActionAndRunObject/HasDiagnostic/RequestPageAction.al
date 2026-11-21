report 50100 MySalesReport
{
    Caption = 'Sales Statistics';
    ProcessingOnly = true;

    requestpage
    {
        actions
        {
            area(Processing)
            {
                action(OpenCustomerList)
                {
                    [|Caption = 'Customers';|]
                    RunObject = page MyCustomerList;
                }
            }
        }
    }
}

page 50101 MyCustomerList
{
    Caption = 'Customers';
}
