codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        MyOtherTable: Record MyOtherTable;
    begin
        MyTable.FindSet();
        repeat
            [|MyOtherTable.CalcFields(OtherFlowField)|];
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

table 50101 MyOtherTable
{
    fields
    {
        field(1; Id; Integer) { }
        field(2; OtherFlowField; Integer)
        {
            FieldClass = FlowField;
            CalcFormula = count(MyOtherTable);
        }
    }

    keys
    {
        key(PK; Id) { Clustered = true; }
    }
}
