codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyBoolean: Boolean)
    [|begin|] // line comment
        if MyBoolean [|then|] // line comment
            exit;

        if MyBoolean [|then|] /* block comment */
            exit;

        if MyBoolean then
        [|begin|] /* block comment */
            Message('Hello');
        end;
    end;
}
