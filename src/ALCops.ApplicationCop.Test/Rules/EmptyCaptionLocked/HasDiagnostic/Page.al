page 50100 MyPage
{
    [|Caption|] = '';

    layout
    {
        area(Content)
        {
            group(MyGroup)
            {
                [|Caption|] = '';

                field(MyField; MyField)
                {
                    [|Caption|] = '';
                }
            }
        }
    }

    actions
    {
        area(Processing)
        {
            action(MyAction)
            {
                [|Caption|] = '';
            }
        }
    }

    var
        MyField: Text;
}