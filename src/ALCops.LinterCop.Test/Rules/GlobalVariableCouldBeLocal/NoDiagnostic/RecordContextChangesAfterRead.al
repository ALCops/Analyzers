codeunit 50100 RecordContextChangesAfterRead
{
    var
        [|MyCustomer|]: Record Customer;

    local procedure ShowCustomer()
    begin
        MyCustomer.Get('10000');
        Message('%1', MyCustomer.Name);
        Clear(MyCustomer);
        MyCustomer.ChangeCompany('CRONUS');
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
