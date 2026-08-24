codeunit 50100 RemoveEventPragmaParameter
{
    [EventSubscriber(ObjectType::Table, Database::"Sales Header", 'OnAfterInsertEvent', '', false, false)]
    local procedure OnAfterInsertSalesHeader(
        var Rec: Record "Sales Header";
        Xyz: Integer)
    begin
        Rec.Init();
        Xyz := 1;
    end;
}