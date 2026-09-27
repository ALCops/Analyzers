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
#pragma warning disable AL0432
    [|Permissions = tabledata Charlie = R,
                  tabledata Bravo = R,
                  tabledata Alpha = R|];
#pragma warning restore AL0432
}
