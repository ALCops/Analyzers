table 50100 MyTable
{
    ObsoleteState = Removed;
    ObsoleteReason = 'Replaced by MyNewTable.';

    fields
    {
        field(1; MyField; Text[100]) { }
    }
}

tableextension 50100 MyTableExt extends MyTable
{
    fields
    {
        field(50100; MyExtField; Text[100])
        {
            [|Caption = 'My Extension Field'|];
        }
    }
}
