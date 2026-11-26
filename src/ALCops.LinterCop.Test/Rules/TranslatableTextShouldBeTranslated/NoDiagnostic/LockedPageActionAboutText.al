page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(MyAction)
            {
                [|AboutText = 'My action explains itself.', Locked = true|];

                trigger OnAction()
                begin
                end;
            }
        }
    }
}
