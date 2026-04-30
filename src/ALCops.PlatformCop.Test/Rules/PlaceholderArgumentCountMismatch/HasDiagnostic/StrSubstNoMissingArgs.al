codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        TestMsg: Label 'It should give an error for this: %1', Comment = '%1=Any text that needs to be tested';
        CompleteText: Text;
    begin
        CompleteText := StrSubstNo([|TestMsg|]);
    end;
}
