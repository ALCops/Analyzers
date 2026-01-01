namespace ALCops.PlatformCop.Test;

codeunit 50100 EventSubscriberCodeunit
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::ALCops.PlatformCop.Test.MyCodeunit, MyProcedure, '', false, false)]
    local procedure MyProcedure(var [|MyTable|]: Record ALCops.PlatformCop.Test.MyTable)
    begin
    end;
}

codeunit 50101 MyCodeunit
{
    [IntegrationEvent(false, false)]
    local procedure MyProcedure(var MyTable: Record MyTable)
    begin
    end;
}

table 50000 MyTable { fields { field(1; MyField; Integer) { } } }