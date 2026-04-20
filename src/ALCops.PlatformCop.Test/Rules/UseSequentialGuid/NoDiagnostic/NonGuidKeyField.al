codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.MyField := [|CreateGuid()|];
        MyTable.Validate(MyField, [|CreateGuid()|]);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
        field(2; MyField; Guid) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
