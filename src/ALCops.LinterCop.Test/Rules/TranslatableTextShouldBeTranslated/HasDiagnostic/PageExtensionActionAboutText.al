pageextension 50100 MyPageExt extends MyPage
{
    actions
    {
        addlast(Processing)
        {
            action(MyExtAction)
            {
                [|AboutText = 'My extension action explains itself.'|];

                trigger OnAction()
                begin
                end;
            }
        }
    }
}

page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
        }
    }
}
