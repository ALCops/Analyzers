codeunit 50111 MyValidScopeLeavingCodeunit
{
    procedure ValidScopeLeaving(var FirstTable: Record "My Test Table"; var SecondTable: Record "My Test Table")
    begin
        if FirstTable.Find('-') then begin
            repeat
                SecondTable."Dummy No. 1" := FirstTable."Dummy No. 1";

                if not SecondTable.Insert() then begin
                    Message('Insert failed');

                    [|Error|]('Failed!');
                end;
            until FirstTable.Next() = 0;
        end;

        Message('Done');

        [|exit|];
    end;

    procedure ExitAsFirstStatement()
    begin
        [|exit|];
    end;

    procedure ErrorAsFirstStatement()
    begin
        [|Error|]('First statement, no before check');
    end;

    procedure CustomErrorMethodNotFlagged()
    begin
        Message('Start');
        [|MyError|]('Not a built-in Error, must not be flagged');
    end;

    // Regression: exit/Error() as a direct else-branch statement must not be treated as a block
    // sibling of the then-branch. Both multi-line and single-line forms are safe.
    procedure ExitAsElseBranchMultiLine(Flag: Boolean)
    begin
        if Flag then begin
            Message('Then');
        end else
            [|exit|];
    end;

    procedure ExitAsElseBranchOneLine(Flag: Boolean)
    begin
        if Flag then Message('Then') else [|exit|];
    end;

    procedure ErrorAsElseBranchMultiLine(Flag: Boolean)
    begin
        if Flag then begin
            Message('Then');
        end else
            [|Error|]('Else branch');
    end;

    procedure ErrorAsElseBranchOneLine(Flag: Boolean)
    begin
        if Flag then Message('Then') else [|Error|]('Else branch');
    end;

    procedure ExitAsThenBranchMultiLine(Flag: Boolean)
    begin
        if Flag then
            [|exit|];
    end;

    procedure ExitAsThenBranchOneLine(Flag: Boolean)
    begin
        if Flag then [|exit|];
    end;

    procedure ErrorAsThenBranchMultiLine(Flag: Boolean)
    begin
        if Flag then
            [|Error|]('Then branch');
    end;

    procedure ErrorAsThenBranchOneLine(Flag: Boolean)
    begin
        if Flag then [|Error|]('Then branch');
    end;

    // Trailing comment on the previous statement does not suppress a real blank line that follows it.
    procedure TrailingCommentThenBlankThenExit()
    begin
        Message('Start'); // trailing comment

        [|exit|];
    end;

    procedure TrailingCommentThenBlankThenError()
    begin
        Message('Start'); // trailing comment

        [|Error|]('boom');
    end;

    local procedure MyError(Msg: Text)
    begin
        Message(Msg);
    end;
}

table 50112 "My Test Table"
{
    fields
    {
        field(1; "Dummy No. 1"; Text[100])
        {
        }
    }
}
