table 50100 MyTable
{
    fields
    {
        field(1; Name; Text[100]) { }
    }
}

report 50100 ParamUsedInReportProcedure
{
    ProcessingOnly = true;

    dataset
    {
        dataitem(MyTable; MyTable)
        {
            trigger OnAfterGetRecord()
            begin
            end;
        }
    }

    procedure SetNameFilter([|NameFilter: Text|])
    begin
        MyTable.SetFilter(Name, NameFilter);
    end;
}
