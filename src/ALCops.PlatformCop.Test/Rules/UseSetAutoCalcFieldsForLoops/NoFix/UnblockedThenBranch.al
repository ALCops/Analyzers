codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Process: Boolean)
    var
        MyTable: Record MyTable;
    begin
        if Process then
            if MyTable.FindSet() then
                repeat
                    [|MyTable.CalcFields(MyFlowField)|];
                until MyTable.Next() = 0;
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
