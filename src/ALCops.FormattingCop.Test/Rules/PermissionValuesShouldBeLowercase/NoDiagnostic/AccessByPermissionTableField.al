table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer)
        {
            [|AccessByPermission = tabledata Bravo = IM|];
        }
    }
}

table 50101 Bravo
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}
