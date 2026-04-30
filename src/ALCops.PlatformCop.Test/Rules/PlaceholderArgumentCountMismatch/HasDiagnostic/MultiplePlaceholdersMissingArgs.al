codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MultiMsg: Label '%1 of %2 records processed for %3.', Comment = '%1=Count,%2=Total,%3=Table name';
        CompleteText: Text;
    begin
        CompleteText := StrSubstNo([|MultiMsg|]);
    end;
}
