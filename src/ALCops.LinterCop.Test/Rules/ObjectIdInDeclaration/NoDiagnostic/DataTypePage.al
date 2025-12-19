codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure()
    begin
        Page.Run([|0|]);
        Page.Run([|0|], MyTable);
        Page.Run([|0|], MyTable, 1);

        Page.RunModal([|0|]);
        Page.RunModal([|0|], MyTable);
        Page.RunModal([|0|], MyTable, 1);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }