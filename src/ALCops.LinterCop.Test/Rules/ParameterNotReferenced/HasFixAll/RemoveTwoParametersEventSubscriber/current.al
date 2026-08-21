codeunit 50100 FixAllEventParameters
{
    [EventSubscriber(ObjectType::Table, Database::"Sales Header", 'OnAfterInsertEvent', '', false, false)]
    local procedure OnAfterInsertSalesHeader(var Rec: Record "Sales Header"; [|RunTrigger: Boolean|]; [|Xyz: Integer|])
    begin
        Rec.Init();
    end;
}
