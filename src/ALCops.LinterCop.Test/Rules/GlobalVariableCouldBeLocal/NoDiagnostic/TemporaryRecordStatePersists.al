codeunit 50100 TemporaryRecordStatePersists
{
    var
        [|MyBuffer|]: Record Buffer temporary;

    local procedure ShowEntryCount()
    begin
        Clear(MyBuffer);
        Message('%1', MyBuffer.Count());
    end;
}

table 50101 Buffer
{
    fields
    {
        field(1; Id; Integer) { }
    }

    keys
    {
        key(PK; Id) { Clustered = true; }
    }
}
