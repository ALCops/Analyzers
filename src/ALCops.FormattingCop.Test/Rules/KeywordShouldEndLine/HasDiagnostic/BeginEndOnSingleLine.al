codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyBoolean: Boolean)
    begin
        if MyBoolean then
            [|begin|] end;
    end;
}
