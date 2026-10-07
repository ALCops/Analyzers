table 50100 MySetup
{
    fields
    {
        [|field(1; Code; Code[10])|]
        {
        }
        field(2; "Some Setting"; Boolean)
        {
        }
    }

    keys
    {
        key(Key1; Code)
        {
            Clustered = true;
        }
    }

    var
        RecordHasBeenRead: Boolean;

    procedure GetRecordOnce()
    begin
        if RecordHasBeenRead then
            exit;
        Get();
        RecordHasBeenRead := true;
    end;
}
