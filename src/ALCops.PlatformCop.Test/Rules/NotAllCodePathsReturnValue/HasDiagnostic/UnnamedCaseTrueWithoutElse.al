codeunit 50100 MyCodeunit
{
    procedure [|Get|](Input: Integer): Integer
    begin
        case true of
            Input = 1:
                exit(1);
            Input = 2:
                exit(2);
        end;
    end;
}