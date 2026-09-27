table 50100 MyTable
{
    ObsoleteState = Removed;
    ObsoleteReason = 'Replaced by MyNewTable.';

    fields
    {
        field(1; MyField; Text[100]) { }
    }

    var
        [|MyGlobalLabel: Label 'Hello World', Locked = true|];
}
