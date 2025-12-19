codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure()
    begin
        Page.Run([|1|]);
        Page.Run([|1|], MyTable);
        Page.Run([|1|], MyTable, 1);
        Page.Run([|50100|]);
        Page.Run([|50100|], MyTable);
        Page.Run([|50100|], MyTable, 1);

        Page.RunModal([|1|]);
        Page.RunModal([|1|], MyTable);
        Page.RunModal([|1|], MyTable, 1);
        Page.RunModal([|50100|]);
        Page.RunModal([|50100|], MyTable);
        Page.RunModal([|50100|], MyTable, 1);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
page 50100 MyPage { }