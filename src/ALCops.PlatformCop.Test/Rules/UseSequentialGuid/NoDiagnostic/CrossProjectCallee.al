codeunit 50110 MyCaller
{
    procedure Execute()
    var
        Worker: Codeunit MyWorker;
        RunId: Guid;
    begin
        RunId := [|CreateGuid()|];
        Worker.Build(RunId);
    end;
}
