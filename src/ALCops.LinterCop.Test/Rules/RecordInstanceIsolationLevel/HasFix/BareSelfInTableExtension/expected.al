tableextension 50001 MyTableExtension extends MyTable
{
    procedure MyProcedure()
    begin
        ReadIsolation(IsolationLevel::UpdLock);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
