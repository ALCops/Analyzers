codeunit 50100 VariableScopeCase05
{
    var
        [|MyProcedureOnlyLabel|]: Label 'Customer: %1', Comment = '%1 = Customer name';

    local procedure ShowCustomerName(CustomerName: Text)
    begin
        Message(MyProcedureOnlyLabel, CustomerName);
    end;
}
