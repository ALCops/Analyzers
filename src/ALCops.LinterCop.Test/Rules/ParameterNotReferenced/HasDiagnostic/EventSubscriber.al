codeunit 50100 EventSubscriberUnusedParameter
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::EventSubscriberUnusedParameter, OnDoSomething, '', false, false)]
    local procedure OnDoSomethingSubscriber([|MyInteger: Integer|]; var IsHandled: Boolean)
    begin
        IsHandled := true;
    end;

    [IntegrationEvent(false, false)]
    internal procedure OnDoSomething(MyInteger: Integer; var IsHandled: Boolean)
    begin
    end;
}
