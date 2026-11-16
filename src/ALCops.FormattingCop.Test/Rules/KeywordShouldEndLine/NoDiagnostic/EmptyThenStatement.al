codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyBoolean: Boolean)
    begin
        if MyBoolean [|then|]; // Empty statement is intentional.
    end;
}
