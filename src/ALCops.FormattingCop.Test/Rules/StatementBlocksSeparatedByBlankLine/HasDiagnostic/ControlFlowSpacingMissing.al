codeunit 50100 MyCodeunit
{
    procedure MissingBeforeAndAfter(Flag: Boolean; Condition: Boolean; Value: Integer; Names: List of [Text])
    var
        Name: Text;
        Counter: Integer;
    begin
        Message('Start');
        [|if|] Flag then begin
            Message('Inside if');
        end;
        [|Message|]('After if');

        Message('Loop prep');
        [|while|] Condition do begin
            Counter += 1;
        end;
        [|Message|]('After while');

        Message('Repeat prep');
        [|repeat|]
            Counter += 1;
        until Counter > 10;
        [|Message|]('After repeat');

        Message('ForEach prep');
        [|foreach|] Name in Names do begin
            Message(Name);
        end;
        [|Message|]('After foreach');

        Message('For prep');
        [|for|] Counter := 10 downto 1 do begin
            Message(Format(Counter));
        end;
        [|Message|]('After for');

        Message('Case prep');
        [|case|] Value of
            1:
                Message('One');
            else
                Message('Other');
        end;
        [|Message|]('After case');
    end;
}
