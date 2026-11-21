report 50100 MyReport
{
    ProcessingOnly = true;
    ToolTip = [|'My ToolTip.'|];

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
