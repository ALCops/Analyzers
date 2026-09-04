codeunit 50100 VariableScopeCase04
{
    var
        [|MyClearedCustomer|]: Record Customer;

    local procedure ShowCustomer(CustomerNo: Code[20])
    begin
        Clear(MyClearedCustomer);

        if MyClearedCustomer.Get(CustomerNo) then
            Message('%1', MyClearedCustomer.Name);
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
