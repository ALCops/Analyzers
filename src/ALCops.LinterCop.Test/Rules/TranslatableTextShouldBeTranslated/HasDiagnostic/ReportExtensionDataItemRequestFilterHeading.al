reportextension 50100 MyReportExt extends MyReport
{
    dataset
    {
        addlast(MyDataItem)
        {
            dataitem(MyExtDataItem; MyTable)
            {
                [|RequestFilterHeading = 'My Extension Data Item Filters'|];
            }
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
