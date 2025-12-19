codeunit 50101 "My Codeunit"
{
    [EventSubscriber(ObjectType::Query, [|50100|], MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyQuery()
    begin
    end;
}

query 50100 MyQuery
{
    elements { dataitem(MyDataItem; MyTable) { column(MyColumn; MyField) { } } }

    [IntegrationEvent(false, false)]
    local procedure MyIntegrationEvent()
    begin
    end;
}