codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.SetLoadFields("Primary Key")|];
        MyTable.Get();

        MyTable.SetLoadFields(); // Removes the previous SetLoadFields, all fields loaded on next read
        MyTable.Get(); // All fields are loaded
        MyTable.Delete();
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
        field(2; MyField; Integer) { }
    }
}
