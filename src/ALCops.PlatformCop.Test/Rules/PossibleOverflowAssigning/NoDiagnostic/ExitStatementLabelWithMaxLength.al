codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text[10]
    var
        MyLabel: Label 'My Label', MaxLength = 10;
    begin
        exit([|MyLabel|]);
end;
}