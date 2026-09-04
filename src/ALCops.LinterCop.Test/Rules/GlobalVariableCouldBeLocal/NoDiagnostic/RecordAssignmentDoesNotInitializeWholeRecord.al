codeunit 50100 RecordAssignmentWholeRecord
{
    var
        [|MyCustomer|]: Record Customer;

    local procedure ShowReplacement(Replacement: Record Customer)
    begin
        MyCustomer := Replacement;
        Message('%1', Format(MyCustomer));
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
