codeunit 50100 RenameTriggerMayReenter
{
    var
        [|MyValue|]: Integer;

    local procedure RenameCustomer(Value: Integer; NewId: Integer)
    var
        Customer: Record Customer;
    begin
        MyValue := Value;
        Customer.Rename(NewId);
        Message('%1', MyValue);
    end;
}

table 50101 Customer
{
    fields
    {
        field(1; Id; Integer) { }
    }

    keys
    {
        key(PK; Id) { Clustered = true; }
    }
}
