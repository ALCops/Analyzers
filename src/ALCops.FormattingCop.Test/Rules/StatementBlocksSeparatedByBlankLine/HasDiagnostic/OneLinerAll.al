codeunit 50120 MyOneLinerAllCodeunit
{
    procedure OneLinerControlFlow(Flag: Boolean; Counter: Integer; Value: Integer)
    begin
        Message('Before if one-liner');
        [|if|] Flag then Message('one-line if');
        [|Message|]('After if one-liner');

        Message('Before for one-liner');
        [|for|] Counter := 1 to 10 do Message(Format(Counter));
        [|Message|]('After for one-liner');

        Message('Before case one-liner');
        [|case|] Value of 1: Message('one'); else Message('other'); end;
        [|Message|]('After case one-liner');
    end;
}
