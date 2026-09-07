codeunit 50100 MyCodeunit
{
    procedure [|GetValue|]() Result: Integer
    var
        Iterator: Integer;
        Buffer: Integer;
    begin
        for Iterator := 1 to ComputeUpperBound(Result) do
            Buffer := Iterator;
    end;

    local procedure ComputeUpperBound(var Value: Integer): Integer
    begin
        Value := 10;
        exit(3);
    end;
}
