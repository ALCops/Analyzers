codeunit 50100 InternalEventIncludesSender
{
    var
        [|MyValue|]: Integer;

    local procedure DoWork()
    begin
        MyValue := 42;
        Message('%1', MyValue);
    end;

    [InternalEvent(true)]
    local procedure OnSomethingHappened()
    begin
    end;
}
