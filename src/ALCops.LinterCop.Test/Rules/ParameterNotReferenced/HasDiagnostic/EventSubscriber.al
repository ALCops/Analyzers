codeunit 50100 MyCodeunit
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyCodeunit, OnDoSomething, '', false, false)]
    local procedure OnDoSomethingSubscriber([|MyInteger: Integer|]; var IsHandled: Boolean)
    begin
        IsHandled := true;
    end;

    [IntegrationEvent(false, false)]
    internal procedure OnDoSomething(MyInteger: Integer; var IsHandled: Boolean)
    begin
    end;
}
