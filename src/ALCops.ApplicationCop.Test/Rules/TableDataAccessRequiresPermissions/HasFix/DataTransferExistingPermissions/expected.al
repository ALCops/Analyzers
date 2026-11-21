codeunit 50000 MyCodeunit
{
    Permissions = tabledata Alpha = rm,
                  tabledata Charlie = r,
                  tabledata Delta = d;

    trigger OnRun()
    var
        MyDataTransfer: DataTransfer;
    begin
        MyDataTransfer.SetTables(Database::Charlie, Database::Alpha);
        MyDataTransfer.AddFieldValue(1, 1);
        MyDataTransfer.CopyFields();
    end;
}

table 50000 Alpha
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