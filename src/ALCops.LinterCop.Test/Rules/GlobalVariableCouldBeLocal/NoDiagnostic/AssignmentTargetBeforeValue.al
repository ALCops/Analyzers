codeunit 50100 AssignmentTargetBeforeValue
{
    var
        [|MyValue|]: Integer;

    local procedure StoreValue()
    var
        Values: array[10] of Integer;
        Worker: Codeunit IndexWorker;
    begin
        MyValue := 42;
        Values[Worker.GetIndex()] := MyValue;
    end;
}

codeunit 50101 IndexWorker
{
    procedure GetIndex(): Integer
    begin
        exit(1);
    end;
}
