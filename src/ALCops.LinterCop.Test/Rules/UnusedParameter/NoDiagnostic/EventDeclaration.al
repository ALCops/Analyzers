codeunit 50100 MyCodeunit
{
    [IntegrationEvent(false, false)]
    internal procedure OnBeforeDoSomething([|var Sender: Codeunit MyCodeunit|]; var IsHandled: Boolean)
    begin
    end;
}
