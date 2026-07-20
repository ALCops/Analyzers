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
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Text[100]) { }
    }
}
