page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(OpenCustomerList)
            {
                [|Caption = 'Customers';|]
                [|ToolTip = 'View the list of customers.';|]
                RunObject = page MyCustomerList;
            }
        }
    }
}

page 50101 MyCustomerList
{
    Caption = 'Customers';
    ToolTip = 'View the list of customers.';
}
