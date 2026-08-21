codeunit 50110 MyValidCodeunit
{
    procedure ValidSpacing(Flag: Boolean; Condition: Boolean; Value: Integer; Names: List of [Text])
    var
        Name: Text;
        Counter: Integer;
    begin
        [|if|] Flag then begin
            Message('First statement in proc, no before check');
        end;

        Message('After if with blank line');

        Message('Before while');

        [|while|] Condition do begin
            [|if|] Counter > 3 then begin
                Message('First statement in while body, no before check');
            end;

            Counter += 1;
        end;

        [|repeat|]
            Counter += 1;
        until Counter > 10;

        Message('After repeat with blank line');

        Message('Before foreach');

        [|foreach|] Name in Names do begin
            Message(Name);
        end;

        Message('Before for');

        [|for|] Counter := 10 downto 1 do begin
            Message(Format(Counter));
        end;

        Message('Before case');

        [|case|] Value of
            1:
                Message('One');
            else
                Message('Other');
        end;

        Message('After case with blank line');
    end;
}
