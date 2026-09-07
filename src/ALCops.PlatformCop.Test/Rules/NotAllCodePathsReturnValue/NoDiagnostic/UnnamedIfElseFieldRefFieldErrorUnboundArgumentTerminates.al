codeunit 50100 MyCodeunit
{
    procedure [|Compute|](Input: Integer): Integer
    var
        FldRef: FieldRef;
    begin
        if Input = 1 then
            exit(10)
        else
            FldRef.FieldError(UndefinedField);
    end;
}