codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure()
    begin
        Codeunit.Run([|1|]);
        Codeunit.Run([|1|], MyTable);
        Codeunit.Run([|50100|]);
        Codeunit.Run([|50100|], MyTable);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }