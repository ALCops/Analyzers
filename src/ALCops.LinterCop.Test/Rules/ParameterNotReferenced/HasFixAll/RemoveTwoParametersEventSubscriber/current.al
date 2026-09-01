codeunit 50100 FixAllEventParameters
{
    [EventSubscriber(ObjectType::Table, Database::"Sales Header", 'OnAfterInsertEvent', '', false, false)]
    local procedure OnAfterInsertSalesHeader(
        var Rec: Record "Sales Header";
        #pragma warning disable AA0024
        [|RunTrigger: Boolean|];
        [|Xyz: Integer|]
        #pragma warning restore AA0024
        )
    begin
        Rec.Init();
    end;
}
