codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyText: Text)
    begin
        case MyText of
            [|'None'|]:
                exit;
            [|'Standard'|], [|'Strong'|]:
                exit;
            [|'Attention'|]..[|'Unfavorable'|]:
                exit;
        end;
    end;
}
