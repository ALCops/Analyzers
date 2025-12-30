codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetAutoCalcFields([|MyTable.MyFlowField|], [|MyTable.MyBlobField|]);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer) { }
        field(2; MyFlowField; Integer)
        {
            FieldClass = FlowField;
        }
        field(3; MyBlobField; Blob) { }
    }

    procedure MyProcedure()
    begin
        Rec.SetAutoCalcFields([|MyFlowField|], [|MyBlobField|]);
    end;
}