table 50100 MyTable
{
    fields
    {
        field(1; MyField; Text[100]) { }
    }

    fieldgroups
    {
        fieldgroup(DropDown; MyField)
        {
            [|Caption = 'My Drop Down'|];
        }
    }
}
