codeunit 50126 MyElseChainBlankValidCodeunit
{
    procedure ElseWithBlankLineAbove(Flag: Boolean)
    begin
        if Flag then begin
            Message('then');
        end

        [|else|] begin
            Message('else with blank line above satisfies RequireBlank');
        end;
    end;

    procedure IfElseAllOnOneLine(Flag: Boolean)
    begin
        // Entire if-else on a single source line: RequireBlank must not fire because
        // the else keyword shares its line with the previous token.
        if Flag then Message('a') [|else|] Message('b');
    end;
}
