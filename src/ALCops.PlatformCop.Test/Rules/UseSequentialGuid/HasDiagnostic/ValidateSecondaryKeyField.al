codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.Validate("Index Field", [|CreateGuid()|]);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
        field(2; "Index Field"; Guid) { }
        field(3; MyField; Guid) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
        key(SK; "Index Field") { }
    }
}
