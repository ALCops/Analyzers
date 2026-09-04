codeunit 50100 VariableScopeCase09
{
    var
        [|MyByReferenceValue|]: Integer;

    local procedure ShowValueChangedByReference()
    begin
        UpdateValue(MyByReferenceValue);
        Message('%1', MyByReferenceValue);
    end;

    local procedure UpdateValue(var Value: Integer)
    begin
        if Value = 0 then
            Value := 42
        else
            Value += 1;
    end;
}
