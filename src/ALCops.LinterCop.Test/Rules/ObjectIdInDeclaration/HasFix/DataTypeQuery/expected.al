codeunit 50100 MyCodeunit
{
    var
        OutStream: OutStream;

    procedure MyProcedure()
    begin
        Query.SaveAsCsv(Query::MyQuery, OutStream, 1);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
query 50100 MyQuery { elements { dataitem(MyDataItem; MyTable) { column(MyColumn; MyField) { } } } }