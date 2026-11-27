codeunit 50100 MyCodeunit
{
    procedure CallMyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.DoSomething([|MyTable|]);
    end;
}
