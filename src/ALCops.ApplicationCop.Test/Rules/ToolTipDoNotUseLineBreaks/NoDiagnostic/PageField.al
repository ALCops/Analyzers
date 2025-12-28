page 50100 MyPage
{
    layout
    {
        area(Content)
        {
            field(MyField; MyField)
            {
                ToolTip = [|'My ToolTip/a forward slash is not an line break'|];
            }
        }
    }

    var
        MyField: Text;
}