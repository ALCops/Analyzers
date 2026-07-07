codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTempTable: Record MyTempTable;
    begin
        while MyTempTable.FindSet() do begin
            [|MyTempTable.CalcFields(MyFlowField)|];
        end;
    end;
}

table 50100 MyTempTable
{
    TableType = Temporary;

    fields
    {
        field(1; Id; Integer) { }
        field(2; MyFlowField; Integer)
        {
            FieldClass = FlowField;
            CalcFormula = count(MyTempTable);
        }
    }

    keys
    {
        key(PK; Id) { Clustered = true; }
    }
}
