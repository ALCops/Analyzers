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
