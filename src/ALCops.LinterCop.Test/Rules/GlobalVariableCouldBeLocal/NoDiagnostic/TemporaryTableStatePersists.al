codeunit 50100 TemporaryTableStatePersists
{
    var
        [|MyBuffer|]: Record Buffer;

    local procedure ShowEntryCount()
    begin
        Clear(MyBuffer);
        Message('%1', MyBuffer.Count());
    end;
}

table 50101 Buffer
{
    TableType = Temporary;

    fields
    {
        field(1; Id; Integer) { }
    }

    keys
    {
        key(PK; Id) { Clustered = true; }
    }
}
