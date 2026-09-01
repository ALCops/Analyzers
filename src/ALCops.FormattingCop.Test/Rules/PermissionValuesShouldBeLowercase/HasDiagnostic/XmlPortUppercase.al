xmlport 50100 "My XmlPort"
{
    [|Permissions = tabledata Alpha = RIMD|];

    schema
    {
        textelement(Root)
        {
        }
    }
}

table 50100 Alpha
{
    Caption = '', Locked = true;
    fields
    {
        field(1; MyField; Integer) { }
    }
}
