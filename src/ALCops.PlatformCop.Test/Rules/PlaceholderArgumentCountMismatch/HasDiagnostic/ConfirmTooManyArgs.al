codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        ConfirmQst: Label 'Do you want to continue?';
    begin
        if Confirm([|ConfirmQst|], true, 'Extra argument') then;
    end;
}
