report 50100 MyReport
{
    ProcessingOnly = true;
    ToolTip = [|'My ToolTip/a forward slash is not an line break.'|];

    requestpage
    {
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
    }

    var
        MyField: Text;
}
