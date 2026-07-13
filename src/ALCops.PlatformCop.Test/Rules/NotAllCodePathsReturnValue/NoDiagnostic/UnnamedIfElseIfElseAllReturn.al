codeunit 50100 MyCodeunit
{
    procedure [|Compute|](Input: Integer): Integer
    begin
        if Input = 1 then
            exit(10)
        else if Input = 2 then
            exit(20)
        else
            exit(30);
    end;
}