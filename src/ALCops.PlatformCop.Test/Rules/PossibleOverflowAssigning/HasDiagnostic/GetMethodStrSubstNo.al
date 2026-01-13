codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        MyCodeA, MyCodeB : Code[20];
    begin
        MyTable.Get([|StrSubstNo('%1%2', MyCodeA, MyCodeB)|]); // Potential overflow combined length exceeds Code[20]
    end;
}

table 50100 MyTable { fields { field(1; MyField; Code[20]) { } } }