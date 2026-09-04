table 50100 TableResetCanClearGlobals
{
    fields
    {
        field(1; Id; Integer) { }
    }

    keys
    {
        key(PK; Id) { Clustered = true; }
    }

    var
        [|MyValue|]: Integer;

    procedure ResetTableState()
    begin
        MyValue := 42;
        Rec.Reset();
        Message('%1', MyValue);
    end;
}
