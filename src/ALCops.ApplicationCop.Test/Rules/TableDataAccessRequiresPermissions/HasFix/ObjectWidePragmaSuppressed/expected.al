codeunit 50000 MyCodeunit
{
    Permissions = tabledata Alpha = r, tabledata Bravo = i;
    procedure ReadAlpha()
    var
        Alpha: Record Alpha;
    begin
        Alpha.FindFirst();
    end;

    procedure ReadBravo()
    var
        Bravo: Record Bravo;
    begin
#pragma warning disable AC0031
        Bravo.FindFirst();
#pragma warning restore AC0031
        Bravo.Insert();
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
