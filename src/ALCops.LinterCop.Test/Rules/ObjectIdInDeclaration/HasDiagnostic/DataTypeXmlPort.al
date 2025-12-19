codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;
        OutStream: OutStream;
        InStream: InStream;

    procedure MyProcedure()
    begin
        Xmlport.Export([|1|], OutStream);
        Xmlport.Export([|1|], OutStream, MyTable);
        Xmlport.Export([|50100|], OutStream);
        Xmlport.Export([|50100|], OutStream, MyTable);

        Xmlport.Import([|1|], InStream);
        Xmlport.Import([|1|], InStream, MyTable);
        Xmlport.Import([|50100|], InStream);
        Xmlport.Import([|50100|], InStream, MyTable);

        Xmlport.Run([|1|]);
        Xmlport.Run([|50100|]);
        Xmlport.Run([|1|], true);
        Xmlport.Run([|50100|], true);
        Xmlport.Run([|1|], true, false, MyTable);
        Xmlport.Run([|50100|], true, false, MyTable);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
xmlport 50100 MyXmlport { }