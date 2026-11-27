codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyText: Text; IsOverdue: Boolean; IsBlocked: Boolean)
    var
        MyStyle: Text;
    begin
        if MyText = (IsOverdue ? (IsBlocked ? [|'Unfavorable'|] : [|'Attention'|]) : [|'Standard'|]) then;
        MyStyle := (MyText = [|'None'|]) ? 'Strong' : 'Subordinate';
    end;
}
