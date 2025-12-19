codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure()
    begin
        Codeunit.Run(Codeunit::MyCodeunit, MyTable);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }