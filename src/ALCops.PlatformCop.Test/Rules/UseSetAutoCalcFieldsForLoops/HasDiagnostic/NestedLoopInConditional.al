codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        OuterTable: Record MyTable;
        InnerTable: Record MyTable;
    begin
        OuterTable.FindSet();
        repeat
            if InnerTable.FindSet() then
                repeat
                    [|InnerTable.CalcFields(MyFlowField)|];
                until InnerTable.Next() = 0;
        until OuterTable.Next() = 0;
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
