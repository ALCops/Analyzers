page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(OpenCustomerList)
            {
                CaptionML = ENU = 'Open Customers', DAN = 'Debitorer';
                RunObject = page MyCustomerList;
            }
        }
    }
}

pageextension 50100 MyPageExtension extends MyPage
{
    actions
    {
        modify(OpenCustomerList)
        {
            [|Caption = 'Customers';|]
        }
    }
}

page 50101 MyCustomerList
{
    Caption = 'Customers';
}
