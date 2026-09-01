codeunit 50100 MyCodeunit
{
    procedure [|Compute|](Count: Integer) Result: Integer
    begin
        while Count > 10 do begin
            Result := Count;
            Count := Count - 1;
        end;
    end;
}