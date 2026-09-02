codeunit 50100 RecordGetThenGetView
{
    var
        [|MyCustomer|]: Record Customer;

    local procedure ShowView()
    begin
        MyCustomer.Get('10000');
        Message('%1', MyCustomer.GetView());
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
