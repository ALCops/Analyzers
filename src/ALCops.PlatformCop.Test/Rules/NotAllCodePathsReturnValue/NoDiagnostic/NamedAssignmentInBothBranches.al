codeunit 50100 MyCodeunit
{
    procedure [|Compute|](UseFirst: Boolean) Result: Integer
    begin
        if UseFirst then
            Result := 1
        else
            Result := 2;
    end;
}