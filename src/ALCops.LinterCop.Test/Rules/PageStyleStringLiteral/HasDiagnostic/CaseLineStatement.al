codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyOption: Integer)
    var
        MyStyle: Text;
    begin
        case MyOption of
            1:
                MyStyle := [|'Standard'|];
        end;
    end;
}
