codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        ProcessedMsg: Label '%1 records have been processed.', Comment = '%1=Count';
    begin
        Message([|ProcessedMsg|]);
    end;
}
