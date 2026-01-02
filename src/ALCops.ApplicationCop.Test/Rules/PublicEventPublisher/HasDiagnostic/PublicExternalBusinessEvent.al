codeunit 50100 MyCodeunit
{
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