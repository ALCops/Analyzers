codeunit 50100 VariableScopeCase03
{
    var
        [|MyCustomerFromGet|]: Record Customer;

    local procedure ShowCustomerFromOriginalPost()
    begin
        MyCustomerFromGet.Get('10000');
        Message('%1', MyCustomerFromGet.Name);
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
