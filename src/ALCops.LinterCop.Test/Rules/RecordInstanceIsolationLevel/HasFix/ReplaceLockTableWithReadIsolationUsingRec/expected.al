table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }

    procedure MyProcedure()
    begin
        Rec.ReadIsolation(IsolationLevel::UpdLock);
    end;
}