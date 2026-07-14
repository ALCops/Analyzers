codeunit 50100 MyCodeunit
{
    procedure [|Compute|](Input: Integer): Integer
    begin
        if Input < 0 then
            Error('negative not supported');

        exit(Input * 2);
    end;
}
