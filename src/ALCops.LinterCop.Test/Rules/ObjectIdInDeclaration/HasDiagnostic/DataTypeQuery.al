codeunit 50100 MyCodeunit
{
    var
        OutStream: OutStream;

    procedure MyProcedure()
    begin
        Query.SaveAsCsv([|1|], OutStream);
        Query.SaveAsCsv([|50100|], OutStream);
        Query.SaveAsCsv([|1|], OutStream, 1);
        Query.SaveAsCsv([|50100|], OutStream, 1);
        Query.SaveAsCsv([|1|], OutStream, 1, 'FormatArgument');
        Query.SaveAsCsv([|50100|], OutStream, 1, 'FormatArgument');

        Query.SaveAsJson([|1|], OutStream);
        Query.SaveAsJson([|50100|], OutStream);

        Query.SaveAsXml([|1|], OutStream);
        Query.SaveAsXml([|50100|], OutStream);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
query 50100 MyQuery { elements { dataitem(MyDataItem; MyTable) { column(MyColumn; MyField) { } } } }