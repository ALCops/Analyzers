page 50100 MyPage
{
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            field(MyField; MyField)
            {
                [|Tooltip = 'This is a tooltip.'|];
            }

            field(SecondField; SecondField)
            {
                [|ToolTip = 'This is also a tooltip.'|];
            }
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Text[100]) { }
        field(2; SecondField; Text[100]) { }
    }
}
