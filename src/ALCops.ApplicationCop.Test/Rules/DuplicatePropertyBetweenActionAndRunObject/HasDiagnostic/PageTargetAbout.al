page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(OpenCustomerList)
            {
                Caption = 'Open Customers';
                [|AboutTitle = 'About customers';|]
                [|AboutText = 'Customers are the people and companies you sell to.';|]
                RunObject = page MyCustomerList;
            }
        }
    }
}

page 50101 MyCustomerList
{
    Caption = 'Customers';
    AboutTitle = 'About customers';
    AboutText = 'Customers are the people and companies you sell to.';
}
