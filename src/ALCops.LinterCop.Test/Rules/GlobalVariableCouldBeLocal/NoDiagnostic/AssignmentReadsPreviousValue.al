codeunit 50100 AssignmentReadsPreviousValue
{
    var
        [|MyValue|]: Integer;

    local procedure IncrementValue()
    begin
        MyValue := MyValue + 1;
        Message('%1', MyValue);
    end;
}
