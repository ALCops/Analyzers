codeunit 50100 MixedProcedureKinds
{
    procedure RegularProcedure(MyInteger: Integer)
    begin
        MyInteger := 1;
    end;

    [EventSubscriber(ObjectType::Table, Database::"Sales Header", 'OnAfterInsertEvent', '', false, false)]
    local procedure SubscriberProcedure(
        var Rec: Record "Sales Header";
        SubscriberUnused: Boolean;
        Xyz: Integer)
    begin
        Rec.Init();
        Xyz := 1;
    end;
}