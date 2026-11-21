page 50100 MyPage
{
    ToolTip = [|'My ToolTip\A new line after a line break.'|];

    layout
    {
        area(Content)
        {
            field(MyField; MyField)
            {
                ToolTip = 'Specifies the value.';
            }
        }
    }

    actions
    {
        area(Processing)
        {
            action(MyAction)
            {
                ToolTip = 'Runs the action.';
            }
        }
    }

    var
        MyField: Text;
}
