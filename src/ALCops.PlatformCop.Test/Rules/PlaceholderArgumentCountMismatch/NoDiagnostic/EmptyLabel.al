codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        EmptyLbl: Label 'No placeholders here.';
        CompleteText: Text;
    begin
        CompleteText := StrSubstNo([|EmptyLbl|]);
    end;
}
