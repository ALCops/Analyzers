codeunit 50100 MyWorker
{
    procedure Build(RunId: Guid)
    var
        MyTable: Record MyTable;
    begin
        MyTable."Run Id" := RunId;
        MyTable.Insert();
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Run Id"; Guid) { }
    }

    keys
    {
        key(PK; "Run Id") { }
    }
}
