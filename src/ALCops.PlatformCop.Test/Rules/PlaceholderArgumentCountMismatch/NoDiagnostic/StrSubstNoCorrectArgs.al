codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        TestMsg: Label 'Hello %1, welcome to %2!', Comment = '%1=User name,%2=Company name';
        CompleteText: Text;
    begin
        CompleteText := StrSubstNo([|TestMsg|], 'World', 'Contoso');
    end;
}
