report 50100 MyReport
{
    ProcessingOnly = true;

    requestpage
    {
        actions
        {
            area(Processing)
            {
                action(MyAction)
                {
                    [|AboutText = 'My request page action explains itself.'|];

                    trigger OnAction()
                    begin
                    end;
                }
            }
        }
    }
}
