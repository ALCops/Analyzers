codeunit 50000 MyCodeunit
{
    Permissions = tabledata MyTable = rm;
    trigger OnRun()
    var
        MyDataTransfer: DataTransfer;
    begin
        MyDataTransfer.SetTables(Database::MyTable, Database::MyTable);
        MyDataTransfer.AddFieldValue(1, 1);
        MyDataTransfer.CopyFields();
    end;
}

table 50000 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}