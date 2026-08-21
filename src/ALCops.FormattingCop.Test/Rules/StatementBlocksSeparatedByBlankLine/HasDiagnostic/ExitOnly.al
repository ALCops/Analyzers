codeunit 50121 MyExitOnlyCodeunit
{
    procedure ExitAfterStatement()
    begin
        Message('Working');
        [|exit|];
    end;

    procedure ExitInsideBranch(Flag: Boolean)
    begin
        if Flag then begin
            Message('Preparing to leave');
            [|exit|];
        end;
    end;
}
