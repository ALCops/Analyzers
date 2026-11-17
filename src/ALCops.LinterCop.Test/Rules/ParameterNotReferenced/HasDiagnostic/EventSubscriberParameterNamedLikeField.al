table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

codeunit 50100 SubscriberParamLikeField
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::SubscriberParamLikeField, OnDoSomething, '', false, false)]
    local procedure OnDoSomethingSubscriber([|Name: Text|]; var IsHandled: Boolean)
    var
        MyTable: Record MyTable;
    begin
        IsHandled := MyTable.Name <> '';
    end;

    [IntegrationEvent(false, false)]
    internal procedure OnDoSomething(Name: Text; var IsHandled: Boolean)
    begin
    end;
}
