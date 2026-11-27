codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyText: Text; IsOverdue: Boolean)
    begin
        if MyText = (IsOverdue ? [|'Unfavorable'|] : [|'Standard'|]) then;
        if MyText in [(IsOverdue ? [|'None'|] : [|'Strong'|])] then;
        case MyText of
            (IsOverdue ? [|'Attention'|] : [|'Favorable'|]):
                exit;
        end;
    end;
}
