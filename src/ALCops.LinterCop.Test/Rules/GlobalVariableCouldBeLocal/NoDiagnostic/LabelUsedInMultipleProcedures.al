codeunit 50100 LabelUsedInMultipleProcedures
{
    var
        [|MyLabel|]: Label 'Hello';

    local procedure ShowFirst()
    begin
        Message(MyLabel);
    end;

    local procedure ShowSecond()
    begin
        Message(MyLabel);
    end;
}
