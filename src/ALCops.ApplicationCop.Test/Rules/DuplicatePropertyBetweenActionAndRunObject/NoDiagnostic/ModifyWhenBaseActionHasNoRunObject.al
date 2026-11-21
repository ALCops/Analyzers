page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(OpenCustomerList)
            {
                trigger OnAction()
                begin
                    Page.Run(Page::MyCustomerList);
                end;
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
