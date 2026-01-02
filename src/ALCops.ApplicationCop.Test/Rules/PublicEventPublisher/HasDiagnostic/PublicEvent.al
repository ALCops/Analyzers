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

    [ExternalBusinessEvent('MyEvent', 'My Event', 'My External Business Event', EventCategory::MyValue)]
    procedure [|MyExternalBusinessEvent|]()
    begin
    end;
}

enum 2000000001 EventCategory { Extensible = true; }
enumextension 50000 EventCategory extends EventCategory
{
    value(0; MyValue) { }
}