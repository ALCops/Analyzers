codeunit 50100 MyCodeunit
{
    [BusinessEvent(false)]
    local procedure [|MyBusinessEvent|]()
    begin
    end;

    [IntegrationEvent(false, false)]
    local procedure [|MyIntegrationEvent|]()
    begin
    end;

    [InternalEvent(false)]
    local procedure [|MyInternalEvent|]()
    begin
    end;
}