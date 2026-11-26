tableextension 50100 MyTableExt extends MyTable
{
    fields
    {
        modify(MyField)
        {
            [|Caption = 'My Modified Field', Locked = true|];
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
