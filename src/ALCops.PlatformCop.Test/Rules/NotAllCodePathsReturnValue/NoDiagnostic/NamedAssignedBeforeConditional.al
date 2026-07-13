codeunit 50100 MyCodeunit
{
    procedure [|Compute|](UseFallback: Boolean) Result: Integer
    begin
        Result := 10;

        if UseFallback then
            Result := 20;
    end;
}