codeunit 50100 ErrorBranchDoesNotContinue
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue(ShouldFail: Boolean)
    begin
        if ShouldFail then
            Error('Stopped');

        MyValue := 10;
        Message('%1', MyValue);
    end;
}
