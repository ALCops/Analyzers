tableextension 50100 MyTableExt extends MyTable
{
    fields
    {
        modify(MyObsoleteField)
        {
            [|Caption = 'My Modified Field'|];
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Text[100]) { }
        field(2; MyObsoleteField; Text[100])
        {
            ObsoleteState = Pending;
            ObsoleteReason = 'Replaced by MyField.';
        }
    }
}
