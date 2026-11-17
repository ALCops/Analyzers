codeunit 50100 MyCodeunit
{
    var
        Continue: Boolean;

    procedure GlobalReferences()
    begin
        [|continue|] := true;
        if [|CONTINUE|] then
            Message(Format([|cOnTiNuE|]));
    end;

    procedure LocalReferences()
    var
        Continue: Boolean;
    begin
        [|continue|] := true;
        if [|CONTINUE|] then
            Message(Format([|cOnTiNuE|]));
    end;
}
