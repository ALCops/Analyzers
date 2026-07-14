codeunit 50100 MyCodeunit
{
    procedure [|Compute|](Input: Integer) Result: Integer
    begin
        if Input = 1 then
            Result := 10
        else
            Error('unsupported');
    end;
}
