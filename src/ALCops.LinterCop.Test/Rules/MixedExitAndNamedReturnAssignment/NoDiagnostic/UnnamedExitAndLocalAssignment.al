codeunit 50100 MyCodeunit
{
    procedure Compute(): Integer
    var
        LocalResult: Integer;
    begin
        [|LocalResult := 1;|]
        [|exit(LocalResult);|]
    end;
}