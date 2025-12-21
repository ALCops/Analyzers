codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        if [|Confirm('Are You Sure?')|] then;
    end;
}