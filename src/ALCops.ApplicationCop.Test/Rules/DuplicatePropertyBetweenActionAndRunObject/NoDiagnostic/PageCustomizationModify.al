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

pagecustomization MyPageCustomization customizes MyPage
{
    actions
    {
        modify(OpenCustomerList)
        {
            [|AboutTitle = 'About customers';|]
        }
    }
}

page 50101 MyCustomerList
{
    Caption = 'Customers';
    AboutTitle = 'About customers';
}
