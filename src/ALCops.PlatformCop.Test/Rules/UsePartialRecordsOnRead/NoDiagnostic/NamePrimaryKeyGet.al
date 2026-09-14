codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.Get('DEFAULT')|];
        exit(MyTable.Description);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; Name; Code[10]) { }
        field(2; Description; Text[100]) { }
    }

    keys
    {
        key(PK; Name) { }
    }
}
