codeunit 50100 InitializedButPassedByVar
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    begin
        MyValue := 10;
        ChangeValue(MyValue);
        Message('%1', MyValue);
    end;

    local procedure ChangeValue(var Value: Integer)
    begin
        Value += 1;
    end;
}
