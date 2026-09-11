page 50100 MyPage
{
    SourceTable = MyTable;

    layout
    {
        area([|content|])
        {
            field(MyField; Rec.MyField)
            {
                ApplicationArea = All;
            }
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
