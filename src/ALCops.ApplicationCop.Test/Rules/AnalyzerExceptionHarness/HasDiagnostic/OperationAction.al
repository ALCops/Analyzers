codeunit 50102 OpThrow
{
    procedure Caller()
    begin
        [|Callee()|];
    end;

    local procedure Callee()
    begin
    end;
}
