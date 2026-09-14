codeunit 50100 MyCodeunit
{
    TableNo = MyTable;

    trigger OnRun()
    begin
        Rec."Primary Key" := [|CreateGuid()|];
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Guid) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
