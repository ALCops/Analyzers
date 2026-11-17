codeunit 50000 MyCodeunit
{
    var
        [|Continue|]: Boolean;

    procedure MyProcedure()
    var
        [|Action|]: Integer;
    begin
        [|Continue|] := true;
        [|Action|] := 1;
        if [|Continue|] then
            Message(Format([|Action|]));
    end;
}
