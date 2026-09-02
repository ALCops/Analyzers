codeunit 50100 AssignmentTargetTraversal
{
    var
        [|MyValue|]: Integer;
        MyIndex: Integer;

    local procedure ShowValue()
    var
        Values: array[10] of Integer;
        Worker: Codeunit IndexWorker;
    begin
        MyValue := 42;
        MyIndex := 1;
        Values[Worker.GetIndex(MyIndex)] := 0;
        Message('%1', MyValue);
    end;
}

codeunit 50101 IndexWorker
{
    procedure GetIndex(Value: Integer): Integer
    begin
        exit(Value);
    end;
}
