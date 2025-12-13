table 50100 MyTable
{
    ObsoleteState = PendingMove;
    MovedTo = '55ffb877-999c-4f22-99d1-4e761b44b757';

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