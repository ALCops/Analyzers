codeunit 50124 MyElseChainCodeunit
{
    procedure ElseBlockWithoutBlankLine(Flag: Boolean)
    begin
        if Flag then begin
            Message('Then branch');
        end
        [|else|] begin
            Message('Else branch on next line, no blank line above');
        end;
    end;

    procedure ElseIfWithoutBlankLine(Flag: Boolean; Other: Boolean)
    begin
        if Flag then begin
            Message('Then');
        end
        [|else|] if Other then begin
            Message('Else-if');
        end;
    end;
}
