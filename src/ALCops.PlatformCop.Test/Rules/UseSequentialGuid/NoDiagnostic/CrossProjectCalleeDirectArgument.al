codeunit 50110 MyCaller
{
    procedure Execute()
    var
        Worker: Codeunit MyWorker;
    begin
        Worker.Build([|CreateGuid()|]);
    end;
}
