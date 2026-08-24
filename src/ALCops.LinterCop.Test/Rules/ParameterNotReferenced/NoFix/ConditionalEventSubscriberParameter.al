codeunit 50100 ConditionalSubscriberNoFix
{
    [EventSubscriber(ObjectType::Table, Database::"Sales Header", 'OnAfterInsertEvent', '', false, false)]
    local procedure OnAfterInsertSalesHeader(
        var Rec: Record "Sales Header";
#if not ACTIVE
        [|RunTrigger: Boolean|];
#else
        InactiveParameter: Date;
#endif
        Xyz: Integer)
    begin
        Rec.Init();
        Xyz := 1;
    end;
}