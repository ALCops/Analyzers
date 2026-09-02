table 50100 MyTable
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

tableextension 50100 MyTableExtension extends MyTable
{
    var
        [|MyValue|]: Integer;

    procedure ResetTableState()
    begin
        MyValue := 42;
        Rec.Reset();
        Message('%1', MyValue);
    end;
}
