codeunit 50100 RecordAssignmentNextCall
{
    var
        [|MyCustomer|]: Record Customer;

    local procedure ShowOrReplaceCustomer(ShowCurrent: Boolean; Replacement: Record Customer)
    begin
        if ShowCurrent then begin
            MyCustomer.Get('10000');
            Message('%1', MyCustomer.Name);
        end else
            MyCustomer := Replacement;
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
