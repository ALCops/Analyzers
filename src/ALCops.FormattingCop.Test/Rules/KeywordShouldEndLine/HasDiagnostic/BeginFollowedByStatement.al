codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyBoolean: Boolean)
    [|begin|] Message('Hello');
    end;

    procedure MyOther(MyBoolean: Boolean)
    begin
        if MyBoolean then
            [|begin|] Message('Hello');
            end;
    end;
}
