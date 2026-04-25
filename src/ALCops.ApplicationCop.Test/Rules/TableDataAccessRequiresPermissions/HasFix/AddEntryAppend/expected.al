codeunit 50000 MyCodeunit
{
    Permissions = tabledata Charlie = r,
                  tabledata Alpha = r,
                  tabledata Bravo = r;

    procedure Test()
    var
        Bravo: Record Bravo;
    begin
        Bravo.FindFirst();
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
