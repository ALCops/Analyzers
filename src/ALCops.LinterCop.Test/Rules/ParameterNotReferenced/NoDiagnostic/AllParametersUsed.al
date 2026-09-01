codeunit 50100 AllParametersUsed
{
    procedure MyProcedure([|MyInteger: Integer|]; [|MyText: Text|])
    begin
        MyInteger := 1;
        MyText := 'Hello';
    end;
}
