codeunit 50100 MyCodeunit
{
    procedure MyProcedure(MyBoolean: Boolean)
    begin
        if MyBoolean [|then|]
            exit;

        if MyBoolean [|then|]
        begin
            Message('Hello');
        end;
    end;
}
