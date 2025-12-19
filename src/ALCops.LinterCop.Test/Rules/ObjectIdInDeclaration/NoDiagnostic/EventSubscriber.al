codeunit 50101 "My Codeunit"
{
    [EventSubscriber(ObjectType::Codeunit, [|Codeunit::MyCodeunit|], MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyCodeunit()
    begin
    end;

    [EventSubscriber(ObjectType::Page, [|Page::MyPage|], MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyPage()
    begin
    end;

    [EventSubscriber(ObjectType::Query, [|Query::MyQuery|], MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyQuery()
    begin
    end;

    [EventSubscriber(ObjectType::Report, [|Report::MyReport|], MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyReport()
    begin
    end;

    [EventSubscriber(ObjectType::Table, [|Database::MyTable|], MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyTable()
    begin
    end;

    [EventSubscriber(ObjectType::XmlPort, [|XmlPort::MyXmlport|], MyIntegrationEvent, '', false, false)]
    local procedure MyEventSubscriberOnMyXmlPort()
    begin
    end;
}

codeunit 50100 MyCodeunit
{
    [IntegrationEvent(false, false)]
    local procedure MyIntegrationEvent()
    begin
    end;
}
page 50100 MyPage
{
    [IntegrationEvent(false, false)]
    local procedure MyIntegrationEvent()
    begin
    end;
}
table 50100 MyTable
{
    fields { field(1; MyField; Integer) { } }

    [IntegrationEvent(false, false)]
    local procedure MyIntegrationEvent()
    begin
    end;
}
report 50100 MyReport
{
    [IntegrationEvent(false, false)]
    local procedure MyIntegrationEvent()
    begin
    end;
}
xmlport 50100 MyXmlport
{
    [IntegrationEvent(false, false)]
    local procedure MyIntegrationEvent()
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