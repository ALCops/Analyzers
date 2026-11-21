page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            group(CustomersGroup)
            {
                [|Caption = 'Customers';|]

                action(OpenCustomerList)
                {
                    Caption = 'Open Customers';
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
