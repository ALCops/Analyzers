codeunit 50100 ErrorInfoWithUsedParameter
{
    procedure MyProcedure([|ErrorInfo: ErrorInfo|]; MyText: Text)
    begin
        MyText := 'Hello';
    end;
}
