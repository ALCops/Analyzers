codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        CompleteText: Text;
    begin
        CompleteText := StrSubstNo([|'Hello %1'|]);
    end;
}
