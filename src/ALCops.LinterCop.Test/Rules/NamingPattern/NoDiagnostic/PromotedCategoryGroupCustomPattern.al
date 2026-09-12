page 50100 MyPage
{
    actions
    {
        area([|Processing|])
        {
            action(actTest)
            {
            }
        }
        area([|Promoted|])
        {
            group([|Category_Process|])
            {
                actionref(actRef; actTest)
                {
                }
            }
        }
    }
}
