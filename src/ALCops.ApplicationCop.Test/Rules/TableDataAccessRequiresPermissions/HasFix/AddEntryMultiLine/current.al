codeunit 50000 MyCodeunit
{
    Permissions = tabledata Alpha = r,
                  tabledata Bravo = i;

    procedure Test()
    var
        Charlie: Record Charlie;
    begin
        [|Charlie.Delete();|]
    end;
}

table 50000 Alpha
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50001 Bravo
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50002 Charlie
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
