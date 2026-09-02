codeunit 50100 RecordGetWithoutKey
{
    var
        [|MyCustomer|]: Record Customer;

    local procedure ReloadCustomer()
    begin
        MyCustomer.Get();
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
