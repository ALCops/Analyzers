codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure()
    begin
        Report.Run([|1|]);
        Report.Run([|50100|]);
        Report.Run([|1|], true);
        Report.Run([|50100|], true);
        Report.Run([|1|], true, false, MyTable);
        Report.Run([|50100|], true, false, MyTable);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
report 50100 MyReport { }