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
            }
        }
    }
}

page 50101 MyCustomerList
{
    Caption = 'Customers';
    ObsoleteState = Pending;
    ObsoleteReason = 'Use the new customer list.';
    ObsoleteTag = '29.0';
}
