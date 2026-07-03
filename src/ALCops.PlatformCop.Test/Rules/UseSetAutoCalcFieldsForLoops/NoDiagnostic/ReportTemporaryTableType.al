report 50100 MyReport
{
    dataset
    {
        dataitem(MyTempTable; MyTempTable)
        {
            trigger OnAfterGetRecord()
            begin
                [|MyTempTable.CalcFields(MyFlowField)|];
            end;
        }
    }
}

table 50100 MyTempTable
{
    TableType = Temporary;

    fields
    {
        field(1; Id; Integer) { }
        field(2; MyFlowField; Integer)
        {
            FieldClass = FlowField;
            CalcFormula = count(MyTempTable);
        }
    }

    keys
    {
        key(PK; Id) { Clustered = true; }
    }
}
