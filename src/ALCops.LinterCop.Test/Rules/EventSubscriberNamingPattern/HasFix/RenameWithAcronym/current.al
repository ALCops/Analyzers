codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"http client handler", OnBeforeSend, '', false, false)]
    local procedure [|BeforeHttp|]()
    begin
    end;
}

codeunit 50101 "http client handler"
{
    [IntegrationEvent(false, false)]
    procedure OnBeforeSend()
    begin
    end;
}
