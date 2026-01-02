codeunit 50100 MyCodeunit
{
    [BusinessEvent(false)]
    procedure [|MyBusinessEvent|]()
    begin
    end;

    [IntegrationEvent(false, false)]
    procedure [|MyIntegrationEvent|]()
    begin
    end;

    [InternalEvent(false)]
    procedure [|MyInternalEvent|]()
    begin
    end;
}