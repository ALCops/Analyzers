page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(OpenCustomerList)
            {
                RunObject = page MyCustomerList;
                ObsoleteState = Pending;
                ObsoleteReason = 'Use the new customer list action.';
                ObsoleteTag = '29.0';
            }
        }
    }
}

pageextension 50100 MyPageExtension extends MyPage
{
    actions
    {
        modify(OpenCustomerList)
        {
            [|Caption = 'Customers';|]
        }
    }
}

page 50101 MyCustomerList
{
    Caption = 'Customers';
}
