codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        [|xSalesLine|]: Record MyTable;
    begin
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
