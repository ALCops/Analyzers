codeunit 50100 MixedProcedureKinds
{
    procedure RegularProcedure(MyInteger: Integer; RegularUnused: Text)
    begin
        MyInteger := 1;
    end;

    [EventSubscriber(ObjectType::Table, Database::"Sales Header", 'OnAfterInsertEvent', '', false, false)]
    local procedure SubscriberProcedure(
        var Rec: Record "Sales Header";
        Xyz: Integer)
    begin
        Rec.Init();
        Xyz := 1;
    end;
}