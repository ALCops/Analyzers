codeunit 50103 MyCodeunit
{
    procedure AssignVariable()
    var
        Continue: Boolean;
    begin
        [|Continue|] := true;
    end;

    procedure SkipIteration()
    var
        i: Integer;
    begin
        for i := 1 to 2 do
            [|continue|];
    end;
}
