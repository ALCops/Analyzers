codeunit 50100 MyUpgrade
{
    Subtype = Upgrade;
    Permissions = tabledata TableA = r, tabledata TableB = m;
    trigger OnRun()
    var
        MyDataTransfer: DataTransfer;
    begin
        MyDataTransfer.SetTables(Database::TableA, Database::TableB);
        MyDataTransfer.AddFieldValue(1, 1);
        MyDataTransfer.CopyFields();
    end;
}

table 50100 TableA
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}

table 50101 TableB
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}