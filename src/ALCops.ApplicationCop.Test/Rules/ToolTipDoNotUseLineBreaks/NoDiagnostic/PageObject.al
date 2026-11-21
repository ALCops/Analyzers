page 50100 MyPage
{
    ToolTip = [|'My ToolTip/a forward slash is not an line break.'|];

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
