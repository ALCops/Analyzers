page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(OpenCustomerList)
            {
                RunObject = page MyCustomerList;
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
            [|ToolTip = 'View the list of customers.';|]
        }
    }
}

page 50101 MyCustomerList
{
    Caption = 'Customers';
    ToolTip = 'View the list of customers.';
    AboutTitle = 'About customers';
    AboutText = 'Customers are the people and companies you sell to.';
}
