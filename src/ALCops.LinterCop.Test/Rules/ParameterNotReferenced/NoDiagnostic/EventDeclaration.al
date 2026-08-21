codeunit 50100 EventDeclarationContract
{
    [IntegrationEvent(false, false)]
    internal procedure OnBeforeDoSomething([|var Sender: Codeunit EventDeclarationContract|]; var IsHandled: Boolean)
    begin
    end;
}
