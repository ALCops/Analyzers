codeunit 50000 MyCodeunit
{
    Permissions = tabledata Alpha = ri, tabledata Bravo = r, tabledata Charlie = rd, tabledata Echo = i;

    procedure InsertAlpha()
    var
        Alpha: Record Alpha;
    begin
        Alpha.FindFirst();
        Alpha.Insert();
    end;

    procedure ReadBravo()
    var
        Bravo: Record Bravo;
    begin
        Bravo.FindFirst();
    end;

    procedure ClearCharlie()
    var
        Charlie: Record Charlie;
    begin
        Charlie.DeleteAll();
    end;

    procedure CopyBravoToEcho()
    var
        MyDataTransfer: DataTransfer;
    begin
        MyDataTransfer.SetTables(Database::Bravo, Database::Echo);
        MyDataTransfer.AddFieldValue(1, 1);
        MyDataTransfer.CopyRows();
    end;
}

codeunit 50001 OtherCodeunit
{
    procedure ReadDelta()
    var
        Delta: Record Delta;
    begin
        Delta.FindFirst();
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

table 50003 Delta
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50004 Echo
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}