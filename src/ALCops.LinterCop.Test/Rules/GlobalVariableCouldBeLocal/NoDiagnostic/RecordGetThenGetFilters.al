codeunit 50100 RecordGetThenGetFilters
{
    var
        [|MyCustomer|]: Record Customer;

    local procedure ShowFilters()
    begin
        MyCustomer.Get('10000');
        Message('%1', MyCustomer.GetFilters());
    end;
}

table 50101 Customer
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }

    keys
    {
        key(PK; "No.") { Clustered = true; }
    }
}
