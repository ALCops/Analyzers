codeunit 50100 RemoveEventPragmaParameter
{
    [EventSubscriber(ObjectType::Table, Database::"Sales Header", 'OnAfterInsertEvent', '', false, false)]
    local procedure OnAfterInsertSalesHeader(
        var Rec: Record "Sales Header";
        #pragma warning disable AA0042
        [|RunTrigger: Boolean|];
        #pragma warning restore AA0042
        Xyz: Integer)
    begin
        Rec.Init();
        Xyz := 1;
    end;
}