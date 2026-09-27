codeunit 50100 MyCodeunit
{
    ObsoleteState = Pending;
    ObsoleteReason = 'Replaced by MyNewCodeunit.';

    procedure MyProcedure()
    var
        [|MyLabel: Label 'Hello World'|];
    begin
    end;
}
