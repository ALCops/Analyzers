page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(OpenCustomerList)
            {
                Image = Customer;
                RunObject = page MyCustomerList;
            }
            action(OpenCustomerListAgain)
            {
                Caption = 'Open Customers';
                RunObject = page MyCustomerList;
            }
            action(OpenCustomerListToo)
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
        modify(OpenCustomerListToo)
        {
            Visible = false;
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
