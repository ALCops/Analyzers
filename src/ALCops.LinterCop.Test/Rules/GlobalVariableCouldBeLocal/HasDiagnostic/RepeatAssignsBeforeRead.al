codeunit 50100 RepeatAssignsBeforeRead
{
    var
        [|MyValue|]: Integer;

    local procedure ShowValues(Maximum: Integer)
    var
        Index: Integer;
    begin
        repeat
            MyValue := Index;
            Message('%1', MyValue);
            Index += 1;
        until Index > Maximum;
    end;
}
