codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyBoolean: Boolean)
    begin
        if MyBoolean then
            Message('Hello')
        else [|begin|]
            Message('World');
        end;
    end;
}
