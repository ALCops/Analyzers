codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text[10]
    var
        MyLabel: Label 'My Label', Locked = true;
    begin
        exit([|MyLabel|]);
end;
}