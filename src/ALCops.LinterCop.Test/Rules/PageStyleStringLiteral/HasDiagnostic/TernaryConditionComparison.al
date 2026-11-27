codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyText: Text)
    var
        MyStyle: Text;
    begin
        MyStyle := (MyText = 'None') ? [|'Strong'|] : [|'Subordinate'|];
    end;
}
