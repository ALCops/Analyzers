codeunit 50100 MyCodeunit
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyCodeunit, OnMyBusinessEvent, '', false, false)]
    local procedure MyProcedure(var [|myTable|]: Record MyTable)
    begin
    end;

    [BusinessEvent(false)]
    local procedure OnMyBusinessEvent(var myTable: Record MyTable)
    begin
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
