table 50100 MyTable
{
    [|Caption = 'My Table'|];
    ObsoleteState = Pending;
    ObsoleteReason = 'Replaced by MyNewTable.';

    fields
    {
        field(1; MyField; Text[100])
        {
            [|Caption = 'My Field'|];
        }
    }
}
