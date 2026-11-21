page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(OpenCustomerList)
            {
                [|Caption = 'Customers';|]
                RunObject = page MyCustomerList;
                ObsoleteState = Pending;
                ObsoleteReason = 'Use the new customer list action.';
                ObsoleteTag = '29.0';
            }
        }
    }
}

page 50101 MyCustomerList
{
    Caption = 'Customers';
}
