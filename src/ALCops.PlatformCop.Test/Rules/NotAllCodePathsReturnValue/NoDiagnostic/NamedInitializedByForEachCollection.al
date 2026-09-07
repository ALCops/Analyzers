codeunit 50100 MyCodeunit
{
    procedure [|GetValue|]() Result: Integer
    var
        Value: Integer;
        Buffer: Integer;
    begin
        foreach Value in BuildList(Result) do
            Buffer := Value;
    end;

    local procedure BuildList(var Total: Integer): List of [Integer]
    var
        Items: List of [Integer];
    begin
        Total := 42;
        exit(Items);
    end;
}
