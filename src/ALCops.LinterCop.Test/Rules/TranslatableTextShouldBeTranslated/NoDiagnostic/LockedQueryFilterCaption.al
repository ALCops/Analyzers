query 50100 MyQuery
{
    elements
    {
        dataitem(MyDataItem; MyTable)
        {
            column(MyField; MyField) { }
            filter(MyFilter; MyOption)
            {
                [|Caption = 'My Filter', Locked = true|];
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
