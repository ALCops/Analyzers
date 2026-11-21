page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(OpenCustomerList)
            {
                Caption = 'Open Customers';
                [|ToolTip = '';|]
                RunObject = page MyCustomerList;
            }
        }
    }
}

page 50101 MyCustomerList
{
    Caption = 'Customers';
    ToolTip = '';
}
