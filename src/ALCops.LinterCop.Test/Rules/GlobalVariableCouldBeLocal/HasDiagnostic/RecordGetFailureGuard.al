codeunit 50100 RecordGetFailureGuard
{
    var
        [|MyCustomer|]: Record Customer;

    local procedure ShowCustomer(CustomerNo: Code[20])
    begin
        if not MyCustomer.Get(CustomerNo) then
            Error('Customer not found');

        Message('%1', MyCustomer.Name);
    end;
}

table 50101 Customer
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Name; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { Clustered = true; }
    }
}
