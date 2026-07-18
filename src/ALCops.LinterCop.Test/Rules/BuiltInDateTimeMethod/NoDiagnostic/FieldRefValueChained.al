table 50100 MyTable
{
    fields
    {
        field(1; "My DateTime"; DateTime) { }
    }
}

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        MyRecRef: RecordRef;
        MyDate: Date;
    begin
        MyTable.FindFirst();

        MyRecRef.GetTable(MyTable);

        MyDate := [|DT2Date(MyRecRef.Field(1).Value)|];
    end;
}
