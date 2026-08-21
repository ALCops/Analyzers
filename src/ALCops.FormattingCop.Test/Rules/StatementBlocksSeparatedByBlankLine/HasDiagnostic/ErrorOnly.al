codeunit 50122 MyErrorOnlyCodeunit
{
    procedure ErrorAfterStatement()
    begin
        Message('Something failed');
        [|Error|]('Boom');
    end;

    procedure ErrorInsideNestedRepeat(var FirstTable: Record "My Test Table"; var SecondTable: Record "My Test Table")
    begin
        if FirstTable.Find('-') then begin
            repeat
                SecondTable."Dummy No. 1" := FirstTable."Dummy No. 1";
                if not SecondTable.Insert() then begin
                    Message('Insert failed');
                    [|Error|]('Fatal');
                end;
            until FirstTable.Next() = 0;
        end;
    end;
}

table 50123 "My Test Table"
{
    fields
    {
        field(1; "Dummy No. 1"; Text[100])
        {
        }
    }
}
