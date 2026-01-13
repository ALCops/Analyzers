codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text[10]
    var
        MyText: Text[10];
        MyLabel: Label 'My Label';
    begin
        MyText := [|MyLabel|];
    end;
}