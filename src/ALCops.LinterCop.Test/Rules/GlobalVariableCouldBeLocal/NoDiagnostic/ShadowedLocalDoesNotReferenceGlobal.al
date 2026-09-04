codeunit 50100 ShadowedLocal
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue()
    var
        MyValue: Integer;
    begin
        MyValue := 10;
        Message('%1', MyValue);
    end;
}
