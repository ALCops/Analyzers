codeunit 50100 BusinessEventIncludesSender
{
    var
        [|MyValue|]: Integer;

    local procedure DoWork()
    begin
        MyValue := 42;
        Message('%1', MyValue);
    end;

    [BusinessEvent(true)]
    local procedure OnSomethingHappened()
    begin
    end;
}
