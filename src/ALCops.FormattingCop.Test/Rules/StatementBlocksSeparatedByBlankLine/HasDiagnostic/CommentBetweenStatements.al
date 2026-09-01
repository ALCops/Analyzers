codeunit 50120 MyCommentBetweenStatements
{
    procedure CommentBeforeControlFlow(Flag: Boolean)
    begin
        Message('Start');
        // ---- section divider ----
        [|if|] Flag then begin
            Message('Found');
        end;
    end;

    procedure CommentBeforeExit()
    begin
        Message('Start');
        // ---- section divider ----
        [|exit|];
    end;

    procedure CommentBeforeError()
    begin
        Message('Start');
        // ---- section divider ----
        [|Error|]('boom');
    end;

    procedure MultipleCommentLinesStillFail()
    begin
        Message('Start');
        // ---- section divider ----
        // second comment line, still no blank
        [|exit|];
    end;

    // Trailing comment on the previous statement line does not turn that line into a separator.
    procedure TrailingCommentAdjacentExit()
    begin
        Message('Start'); // trailing comment on same line
        [|exit|];
    end;

    procedure TrailingCommentAdjacentControlFlow(Flag: Boolean)
    begin
        Message('Start'); // trailing comment on same line
        [|if|] Flag then begin
            Message('Found');
        end;
    end;
}
