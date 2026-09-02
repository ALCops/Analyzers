codeunit 50100 ArrayElementStateNotModeled
{
    var
        [|MyNumbers|]: array[2] of Integer;

    local procedure RebuildNumbers()
    begin
        Clear(MyNumbers);
        MyNumbers[1] := 1;
        Message('%1', MyNumbers[1]);
    end;
}
