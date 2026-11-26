page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            customaction(MyCustomAction)
            {
                CustomActionType = Flow;
                FlowId = '00000000-0000-0000-0000-000000000001';
                FlowEnvironmentId = '00000000-0000-0000-0000-000000000002';
                [|AboutTitle = 'About my custom action', Locked = true|];
            }
        }
    }
}
