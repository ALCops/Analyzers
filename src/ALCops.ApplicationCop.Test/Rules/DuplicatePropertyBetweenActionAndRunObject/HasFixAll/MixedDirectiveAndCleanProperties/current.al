page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(OpenCustomerList)
            {
                Image = Customer;
#pragma warning disable AA0218
                Visible = true;
#pragma warning restore AA0218
                [|Caption = 'Customers';|]
                [|ToolTip = 'View the list of customers.';|]
                RunObject = page MyCustomerList;
            }
            action(OpenCustomerListAgain)
            {
                [|Caption = 'Customers';|]
                [|AboutTitle = 'About customers';|]
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
