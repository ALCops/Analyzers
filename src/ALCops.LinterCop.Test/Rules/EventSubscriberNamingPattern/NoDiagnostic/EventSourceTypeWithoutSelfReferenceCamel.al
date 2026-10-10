codeunit 50100 "Self Publisher"
{
    [InternalEvent(true, false)]
    local procedure OnGetSelf()
    begin
    end;

    [EventSubscriber(ObjectType::Codeunit, Codeunit::"Self Publisher", OnGetSelf, '', false, false)]
    local procedure [|"(codeunit)"|]()
    begin
    end;
}

codeunit 50101 "External Publisher"
{
    [IntegrationEvent(false, false)]
    procedure OnGetExternal()
    begin
    end;
}

codeunit 50102 ExternalSubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::"External Publisher", OnGetExternal, '', false, false)]
    local procedure [|"(codeunit)"|]()
    begin
    end;
}
