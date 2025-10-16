table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
        [|field(2; MyCalcField; Boolean)|]
        {
            FieldClass = FlowField;
            CalcFormula = exist(MyTable where (MyField = field(MyField)));
            Editable = false;
        }
    }
}