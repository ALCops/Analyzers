page 50100 MyPage
{
    ObsoleteState = Pending;
    ObsoleteReason = 'Use the new page.';
    ObsoleteTag = '29.0';

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
}
