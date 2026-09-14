namespace MyPublisher.MyExtension.MyAppDomain;

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyPublisher.MyExtension.MyAppDomain.MyTable;
    begin
        MyTable."Primary Key" := [|CreateGuid()|];
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Guid) { }
    }
}
