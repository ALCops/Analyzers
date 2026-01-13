codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.Get([|'ABCDEFGHIJKLMNOPQRSTU'|]); // 21 characters, exceeds Code[20]
    end;
}

table 50100 MyTable { fields { field(1; MyField; Code[20]) { } } }