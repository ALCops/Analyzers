page 50100 MyPage
{
    actions
    {
        area(Processing)
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
    CaptionML = ENU = 'Customers', DAN = 'Debitorer';
}
