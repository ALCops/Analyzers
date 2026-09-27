page 50100 MyPage
{
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            field(MyField; Rec.MyField)
            {
                [|ToolTip = 'This is a tooltip'|];
                ObsoleteState = Pending;
                ObsoleteReason = 'Replaced by MyNewField.';
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
