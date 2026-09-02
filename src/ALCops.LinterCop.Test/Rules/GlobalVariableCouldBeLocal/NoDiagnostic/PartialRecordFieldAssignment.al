codeunit 50100 PartialRecordFieldAssignment
{
    var
        [|MyCustomer|]: Record Customer;

    local procedure ShowCustomer()
    begin
        MyCustomer.Name := 'New name';
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
}
