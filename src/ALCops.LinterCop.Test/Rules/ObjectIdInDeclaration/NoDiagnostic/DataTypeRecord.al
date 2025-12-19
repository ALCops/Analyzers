codeunit 50100 MyCodeunit
{
    var
        Field: Record MyTable;

    procedure MyProcedure()
    begin
        Field.FilterGroup([|0|]);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }