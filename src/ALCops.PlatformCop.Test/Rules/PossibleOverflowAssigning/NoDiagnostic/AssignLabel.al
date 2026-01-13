codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text[10]
    var
        MyText: Text[10];
        MyLabel: Label 'My Label longer than 10 characters';
    begin
        MyText := [|MyLabel|];
    end;
}