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
}
