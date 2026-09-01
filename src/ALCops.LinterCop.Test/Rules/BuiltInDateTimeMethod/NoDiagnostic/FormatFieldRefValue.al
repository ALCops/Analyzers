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
        MyFieldRef: FieldRef;
        MyText: Text;
    begin
        MyTable.FindFirst();

        MyRecRef.GetTable(MyTable);
        MyFieldRef := MyRecRef.Field(1);

        MyText := [|Format(MyFieldRef.Value, 0, '<HOURS24>')|];
    end;
}
