codeunit 50100 RecordGetThenFlowFilter
{
    var
        [|MyCustomer|]: Record Customer;

    local procedure ShowDateFilter()
    begin
        MyCustomer.Get('10000');
        Message('%1', MyCustomer."Date Filter");
    end;
}

table 50101 Customer
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; "Date Filter"; Date) { FieldClass = FlowFilter; }
    }

    keys
    {
        key(PK; "No.") { Clustered = true; }
    }
}
