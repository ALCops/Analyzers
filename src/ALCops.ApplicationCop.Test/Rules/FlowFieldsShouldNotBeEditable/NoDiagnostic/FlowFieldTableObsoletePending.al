table 50100 MyTable
{
    ObsoleteState = Pending;
    MovedFrom = '308073ce-06ea-40a0-aa5d-dae921a17d55';

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