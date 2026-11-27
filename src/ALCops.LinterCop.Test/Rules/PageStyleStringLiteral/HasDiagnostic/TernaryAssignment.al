codeunit 50100 MyCodeunit
{
    procedure MyProcedure(IsOverdue: Boolean)
    var
        MyStyle: Text;
    begin
        MyStyle := IsOverdue ? [|'Unfavorable'|] : [|'Favorable'|];
    end;
}
