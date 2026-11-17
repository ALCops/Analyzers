codeunit 50103 MyCodeunit
{
    procedure AssignVariable()
    var
        Continue: Boolean;
    begin
        [|continue|] := true;
    end;

    procedure SkipIteration()
    var
        i: Integer;
    begin
        for i := 1 to 2 do
            [|CONTINUE|];
    end;
}
