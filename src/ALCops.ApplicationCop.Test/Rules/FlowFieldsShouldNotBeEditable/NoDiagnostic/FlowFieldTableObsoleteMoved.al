table 50100 MyTable
{
    ObsoleteState = Moved;
    MovedTo = '56923052-1508-4f47-a102-5ad0ab8268c9';

    fields
    {
        field(1; MyField; Integer) { }
        [|field(2; MyCalcField; Boolean)|]
        {
            FieldClass = FlowField;
            CalcFormula = exist(MyTable where (MyField = field(MyField)));
        }
    }
}