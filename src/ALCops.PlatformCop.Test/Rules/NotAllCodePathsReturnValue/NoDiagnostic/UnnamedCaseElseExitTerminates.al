codeunit 50100 MyCodeunit
{
    procedure [|Compute|](Input: Integer): Integer
    begin
        case Input of
            1:
                exit(10);
            2:
                exit(20);
            else
                exit(30);
        end;
    end;
}
