codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text[10]
    var
        MyLabel: Label 'My Label longer than 10 characters';
    begin
        exit([|MyLabel|]);
end;
}