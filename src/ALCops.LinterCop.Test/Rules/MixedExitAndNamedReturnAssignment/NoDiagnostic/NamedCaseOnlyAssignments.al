codeunit 50100 MyCodeunit
{
    procedure Compute(Input: Integer) Result: Integer
    begin
        [|case Input of
            1:
                Result := 10;
            2:
                Result := 20;
            else
                Result := 30;
        end;|]
    end;
}