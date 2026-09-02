codeunit 50100 ConditionalReadBranch
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue(ShouldShow: Boolean)
    begin
        if ShouldShow then begin
            MyValue := 10;
            Message('%1', MyValue);
        end;
    end;
}
