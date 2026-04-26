codeunit 50000 MyCodeunit
{
    Permissions = tabledata Alpha = r,
                  tabledata Charlie = r,
                  tabledata Delta = r;

    procedure Test()
    var
        Alpha: Record Alpha;
    begin
        Alpha.FindFirst();
    end;
}

table 50000 Alpha
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50001 Charlie
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50002 Delta
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
