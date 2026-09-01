codeunit 50100 MyCodeunit
{
    procedure [|Compute|](SetResult: Boolean) Result: Integer
    begin
        if SetResult then
            Result := 1;
    end;
}