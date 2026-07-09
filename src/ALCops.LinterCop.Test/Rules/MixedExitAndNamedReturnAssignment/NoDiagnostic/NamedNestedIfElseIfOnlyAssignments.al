codeunit 50100 MyCodeunit
{
    procedure Compute(Outer: Boolean; Inner: Integer) Result: Integer
    begin
        [|if Outer then begin
        if Inner = 1 then
            Result := 10
        else if Inner = 2 then
            Result := 20
        else
            Result := 40;
    end else
            Result := 30;|]
    end;
}