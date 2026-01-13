codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text[10]
    var
        MyLabel: Label 'My Label';
    begin
        exit([|MyLabel|]);
end;
}