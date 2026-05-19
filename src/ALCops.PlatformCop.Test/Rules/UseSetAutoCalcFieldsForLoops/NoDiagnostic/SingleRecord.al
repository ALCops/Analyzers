codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.Get(1);
        [|MyTable.CalcFields(MyFlowField)|];
        Message('%1', MyTable.MyFlowField);
    end;
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
