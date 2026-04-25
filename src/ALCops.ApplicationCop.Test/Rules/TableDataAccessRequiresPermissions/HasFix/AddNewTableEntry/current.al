codeunit 50000 MyCodeunit
{
    Permissions = tabledata MyTableOne = r;

    procedure Test()
    var
        MyTableTwo: Record MyTableTwo;
    begin
        [|MyTableTwo.Insert();|]
    end;
}

table 50000 MyTableOne
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50001 MyTableTwo
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
