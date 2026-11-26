report 50100 MyReport
{
    dataset
    {
        dataitem(MyDataItem; MyTable)
        {
            column(MyOptionColumn; MyOption)
            {
                [|OptionCaption = 'First,Second', Locked = true|];
            }
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
