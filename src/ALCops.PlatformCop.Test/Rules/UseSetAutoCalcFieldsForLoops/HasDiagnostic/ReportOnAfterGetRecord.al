report 50100 MyReport
{
    dataset
    {
        dataitem(MyTable; MyTable)
        {
            trigger OnAfterGetRecord()
            begin
                [|MyTable.CalcFields(MyFlowField)|];
            end;
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; Id; Integer) { }
        field(2; MyFlowField; Integer)
        {
            FieldClass = FlowField;
            CalcFormula = count(MyTable);
        }
    }

    keys
    {
        key(PK; Id) { Clustered = true; }
    }
}
