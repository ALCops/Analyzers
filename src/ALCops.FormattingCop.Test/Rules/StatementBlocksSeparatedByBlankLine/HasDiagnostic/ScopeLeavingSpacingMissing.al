codeunit 50101 MyScopeLeavingCodeunit
{
    procedure MissingBeforeScopeLeaving(var FirstTable: Record "My Test Table"; var SecondTable: Record "My Test Table")
    begin
        if FirstTable.Find('-') then begin
            repeat
                SecondTable."Dummy No. 1" := FirstTable."Dummy No. 1";
                [|if|] not SecondTable.Insert() then begin
                    Message('Insert failed');
                    [|Error|]('Failed!');
                end;
            until FirstTable.Next() = 0;
        end;

        Message('Done');
        [|exit|];
    end;

    procedure MissingBeforeError()
    begin
        Message('Start');
        [|Error|]('Boom');
    end;
}

table 50102 "My Test Table"
{
    fields
    {
        field(1; "Dummy No. 1"; Text[100])
        {
        }
    }
}
