
codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.[|Insert|](true);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }