report 50100 MyReport
{
    dataset
    {
        dataitem(MyTable; MyTable)
        {
            trigger OnAfterGetRecord()
            begin
                "Primary Key" := [|CreateGuid()|];
            end;
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Guid) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}
