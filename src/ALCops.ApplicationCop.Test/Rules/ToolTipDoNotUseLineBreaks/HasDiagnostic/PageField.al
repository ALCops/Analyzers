page 50100 MyPage
{
    layout
    {
        area(Content)
        {
            field(MyField; MyField)
            {
                ToolTip = [|'My ToolTip\A new line after a line break'|];
            }
        }
    }

    var
        MyField: Text;
}