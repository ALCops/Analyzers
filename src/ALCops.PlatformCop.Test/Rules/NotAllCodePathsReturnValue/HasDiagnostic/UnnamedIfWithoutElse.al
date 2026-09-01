codeunit 50100 MyCodeunit
{
    procedure [|GetValue|](IsPositive: Boolean): Integer
    begin
        if IsPositive then
            exit(1);
    end;
}