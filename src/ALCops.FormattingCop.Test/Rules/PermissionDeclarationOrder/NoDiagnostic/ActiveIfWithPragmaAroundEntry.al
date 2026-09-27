table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50101 Bravo
{
    Caption = '', Locked = true;
    ObsoleteState = Pending;
    ObsoleteReason = 'Replaced by table Alpha.';
    ObsoleteTag = '25.0';
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50102 Charlie
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}

permissionset 50100 "My Permission Set"
{
    Assignable = true;
    [|Permissions = tabledata Charlie = R,
#if not CLEAN25
#pragma warning disable AL0432
                  tabledata Bravo = R,
#pragma warning restore AL0432
#endif
                  tabledata Alpha = R|];
}
