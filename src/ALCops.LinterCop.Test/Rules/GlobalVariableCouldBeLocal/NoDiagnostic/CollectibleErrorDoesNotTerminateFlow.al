codeunit 50100 CollectibleErrorContinues
{
    var
        [|MyValue|]: Integer;

    [ErrorBehavior(ErrorBehavior::Collect)]
    local procedure ShowValue(ShouldFail: Boolean)
    begin
        if ShouldFail then
            Error(ErrorInfo.Create('Collected error', true))
        else
            MyValue := 42;

        Message('%1', MyValue);
        ClearCollectedErrors();
    end;
}
