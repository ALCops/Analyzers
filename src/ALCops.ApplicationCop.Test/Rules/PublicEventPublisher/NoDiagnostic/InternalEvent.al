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

    [ExternalBusinessEvent('MyEvent', 'My Event', 'My External Business Event', EventCategory::MyValue)]
    internal procedure [|MyExternalBusinessEvent|]()
    begin
    end;
}

enum 2000000001 EventCategory { Extensible = true; }
enumextension 50000 EventCategory extends EventCategory
{
    value(0; MyValue) { }
}