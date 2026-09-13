table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }

    procedure MyProcedure()
    begin
        this.ReadIsolation(IsolationLevel::UpdLock);
    end;
}
