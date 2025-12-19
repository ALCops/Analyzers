codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        RecordRef: RecordRef;
        FieldRef: FieldRef;
    begin
        FieldRef := RecordRef.FieldIndex([|1|]);
    end;
}