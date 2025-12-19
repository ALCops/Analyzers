codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure()
    begin
        Xmlport.Run(Xmlport::MyXmlport, true, false, MyTable);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
xmlport 50100 MyXmlport { }