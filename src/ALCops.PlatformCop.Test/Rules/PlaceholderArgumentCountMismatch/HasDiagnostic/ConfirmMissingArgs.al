codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        ConfirmQst: Label 'Do you want to delete %1?', Comment = '%1=Record description';
    begin
        if Confirm([|ConfirmQst|]) then;
    end;
}
