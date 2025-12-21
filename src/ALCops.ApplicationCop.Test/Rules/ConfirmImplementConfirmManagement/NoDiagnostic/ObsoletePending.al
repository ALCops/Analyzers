codeunit 50100 MyCodeunit
{
    ObsoleteState = Pending;

    local procedure MyProcedure()
    begin
        if [|Confirm('Are You Sure?')|] then;
    end;
}