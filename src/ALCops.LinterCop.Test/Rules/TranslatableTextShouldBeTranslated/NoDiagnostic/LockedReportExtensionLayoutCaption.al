reportextension 50100 MyReportExt extends MyReport
{
    rendering
    {
        layout(MyExtLayout)
        {
            Type = Excel;
            LayoutFile = 'MyExtLayout.xlsx';
            [|Caption = 'My Extension Layout', Locked = true|];
        }
    }
}

report 50100 MyReport
{
    dataset
    {
        dataitem(MyDataItem; MyTable)
        {
            column(MyField; MyField) { }
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Text[100]) { }
        field(2; MyOption; Option)
        {
            OptionMembers = First,Second;
        }
    }
}
