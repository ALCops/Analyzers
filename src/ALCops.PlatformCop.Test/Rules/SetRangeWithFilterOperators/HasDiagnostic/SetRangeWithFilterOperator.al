codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        MyCode: Code[20];
    begin
        [|MyTable.SetRange(MyField, '<>%1', MyCode)|];
    end;
}

table 50100 MyTable { fields { field(1; MyField; Code[20]) { } } }
