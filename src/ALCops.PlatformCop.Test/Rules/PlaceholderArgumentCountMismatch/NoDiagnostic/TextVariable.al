codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyText: Text;
        CompleteText: Text;
    begin
        MyText := 'Hello %1';
        CompleteText := StrSubstNo([|MyText|]);
    end;
}
