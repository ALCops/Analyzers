codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.FindSet();
        repeat
            [|MyCalcFieldsProcedure(MyTable)|];
        until MyTable.Next() = 0;
    end;

    local procedure MyCalcFieldsProcedure(var MyTable: Record MyTable)
    begin
        MyTable.CalcFields(MyFlowField);
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
