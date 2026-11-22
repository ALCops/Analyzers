page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
        }
    }
}

pageextension 50100 MyPageExtension extends MyPage
{
    actions
    {
        addlast(Processing)
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
    ToolTip = 'View the list of customers.';
    AboutTitle = 'About customers';
    AboutText = 'Customers are the people and companies you sell to.';
}
