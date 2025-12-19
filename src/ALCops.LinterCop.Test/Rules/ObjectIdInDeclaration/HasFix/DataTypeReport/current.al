codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure()
    begin
        Report.Run([|50100|], true, false, MyTable);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
report 50100 MyReport { }