tableextension 50100 MyTableExt extends MyTable
{
    fields
    {
        modify(MyField)
        {
            [|Caption = 'My Modified Field'|];
        }
    }
}

table 50100 MyTable
{
    ObsoleteState = Removed;
    ObsoleteReason = 'Replaced by MyNewTable.';

    fields
    {
        field(1; MyField; Text[100]) { }
    }
}
