codeunit 50100 MyCodeunit
{
    procedure [|GetValue|](Kind: Integer) Result: Integer
    var
        Buffer: Integer;
    begin
        case DetermineKind(Kind, Result) of
            1:
                Buffer := 1;
            2:
                Buffer := 2;
        end;
    end;

    local procedure DetermineKind(Kind: Integer; var Value: Integer): Integer
    begin
        Value := Kind;
        exit(Kind);
    end;
}
