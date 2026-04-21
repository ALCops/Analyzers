codeunit 50100 MyCodeunit
{
    [EventSubscriber(ObjectType::Table, Database::MyTable, OnAfterModifyEvent, '', false, false)]
    local procedure OnAfterModifyEventOnMyTable(var [|xRec|]: Record MyTable)
    begin
    end;
}

table 50100 MyTable { fields { field(1; MyField; Integer) { } } }
