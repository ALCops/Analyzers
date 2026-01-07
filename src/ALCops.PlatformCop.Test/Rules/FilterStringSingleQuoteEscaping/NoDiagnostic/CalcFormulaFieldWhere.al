table 50100 MyTable
{
    fields
    {
        field(1; MyField; Code[10]) { }
        field(2; MyFlowField; Code[20])
        {
            FieldClass = FlowField;
            CalcFormula = lookup(MyTable.MyField where(MyField = filter([|'<>'''''|])));
        }
    }
}