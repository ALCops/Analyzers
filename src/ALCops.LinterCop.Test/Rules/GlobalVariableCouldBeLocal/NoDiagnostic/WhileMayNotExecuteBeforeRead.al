codeunit 50100 WhileMayNotExecuteBeforeRead
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue(ShouldRun: Boolean)
    begin
        while ShouldRun do begin
            MyValue := 10;
            ShouldRun := false;
        end;

        Message('%1', MyValue);
    end;
}
