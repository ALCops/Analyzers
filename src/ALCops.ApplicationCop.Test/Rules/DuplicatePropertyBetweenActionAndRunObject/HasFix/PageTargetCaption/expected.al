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
