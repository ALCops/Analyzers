codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.MyGuidField := [|CreateGuid()|];
    end;
}

tableextension 50101 OtherTableExtension extends OtherTable
{
    keys
    {
        key(GuidKey; MyGuidField) { }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; MyGuidField; Guid) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}

table 50101 OtherTable
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(2; MyGuidField; Guid) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
