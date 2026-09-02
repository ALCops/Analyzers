codeunit 50100 UnknownExternalCallMayReenter
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    var
        Worker: Codeunit ExternalWorker;
    begin
        MyValue := 10;
        Worker.DoWork();
        Message('%1', MyValue);
    end;
}

codeunit 50101 ExternalWorker
{
    procedure DoWork()
    begin
    end;
}
