codeunit 50100 ContinueSkipsInitialization
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValue(Skip: Boolean)
    begin
        repeat
            if Skip then
                continue;

            MyValue := 42;
        until true;

        Message('%1', MyValue);
    end;
}
