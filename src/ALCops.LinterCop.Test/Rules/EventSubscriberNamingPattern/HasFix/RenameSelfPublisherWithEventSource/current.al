codeunit 50100 "Self Publisher"
{
    [InternalEvent(true, false)]
    local procedure OnGetSelf()
    begin
    end;

    [EventSubscriber(ObjectType::Codeunit, Codeunit::"Self Publisher", OnGetSelf, '', false, false)]
    local procedure [|"(Codeunit Self Publisher) OnGetSelf"|]()
    begin
    end;
}