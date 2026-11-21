page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(OpenCustomerList)
            {
                Caption = 'Open Customers';
                RunObject = page MyCustomerList;
            }
        }
        area(Promoted)
        {
            actionref(OpenCustomerList_Promoted; OpenCustomerList)
            {
                [|Visible = true;|]
            }
        }
    }
}

page 50101 MyCustomerList
{
    Caption = 'Customers';
}
