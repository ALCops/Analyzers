codeunit 50100 BreakMaySkipInitialization
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue(StopImmediately: Boolean)
    begin
        repeat
            if StopImmediately then
                break;

            MyValue := 10;
        until true;

        Message('%1', MyValue);
    end;
}
