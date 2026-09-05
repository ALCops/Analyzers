codeunit 50101 MyHandler
{
    procedure Error()
    begin
    end;
}

codeunit 50100 MyCodeunit
{
    procedure [|Compute|](Input: Integer): Integer
    var
        Handler: Codeunit MyHandler;
    begin
        if Input = 1 then
            exit(10)
        else
            Handler.Error(UndefinedValue);
    end;
}