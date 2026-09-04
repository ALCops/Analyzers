codeunit 50100 RecordGetWholeRecordArg
{
    var
        [|MyCustomer|]: Record Customer;

    local procedure ConsumeCustomer()
    begin
        MyCustomer.Get('10000');
        Consume(MyCustomer);
    end;

    local procedure Consume(CustomerValue: Record Customer)
    begin
        Message('%1', CustomerValue.Name);
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
