codeunit 50100 MyCodeunit
{
    procedure [|Compute|](Input: Integer): Integer
    begin
        if Input = 1 then
            exit(10)
        else
            FieldError(Input);
    end;

    local procedure FieldError(Input: Integer)
    begin
    end;
}
