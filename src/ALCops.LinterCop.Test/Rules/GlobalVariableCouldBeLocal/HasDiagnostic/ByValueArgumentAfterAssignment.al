codeunit 50100 ByValueArgumentAfterAssignment
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    begin
        MyValue := 10;
        Consume(MyValue);
    end;

    local procedure Consume(Value: Integer)
    begin
        Message('%1', Value);
    end;
}
