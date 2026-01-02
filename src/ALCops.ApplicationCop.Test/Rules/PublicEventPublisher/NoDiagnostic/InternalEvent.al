codeunit 50100 MyCodeunit
{
    [BusinessEvent(false)]
    internal procedure [|MyBusinessEvent|]()
    begin
    end;

    [IntegrationEvent(false, false)]
    internal procedure [|MyIntegrationEvent|]()
    begin
    end;

    [InternalEvent(false)]
    internal procedure [|MyInternalEvent|]()
    begin
    end;
}