codeunit 50100 MyCodeunit
{
    var
        RecordRef: RecordRef;

    procedure MyProcedure()
    begin
        RecordRef.Open(Database::MyTable);
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }