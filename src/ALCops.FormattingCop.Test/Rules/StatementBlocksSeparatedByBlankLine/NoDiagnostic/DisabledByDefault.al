codeunit 50125 MyDisabledByDefaultCodeunit
{
    procedure OneLinerControlFlowNotFlagged(Flag: Boolean; Counter: Integer)
    begin
        Message('Before one-liner');
        [|if|] Flag then Message('one-line if is excluded by default OneLinerMode=None');
        [|for|] Counter := 1 to 3 do Message(Format(Counter));
        Message('After one-liner block');
    end;

    procedure CaseBranchSpacingNotFlagged(Value: Integer)
    begin
        Message('Before case');

        case Value of
            [|1|]:
                Message('One');
            [|2|]:
                Message('Two adjacent, no blank line between branches');
            [|else|]
                Message('Else adjacent to previous branch');
        end;

        Message('After case');
    end;

    procedure ElseKeywordNotFlaggedByDefault(Flag: Boolean)
    begin
        Message('Before if');

        if Flag then begin
            Message('then branch');
        end
        [|else|] begin
            Message('else with no blank line above is fine when ElseChainBeforeMode=Off');
        end;

        Message('After if-else');
    end;
}
