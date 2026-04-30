codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        ConfirmQst: Label 'Do you want to delete %1?', Comment = '%1=Record description';
    begin
        if Confirm([|ConfirmQst|], true, 'Sales Order 1001') then;
    end;
}
