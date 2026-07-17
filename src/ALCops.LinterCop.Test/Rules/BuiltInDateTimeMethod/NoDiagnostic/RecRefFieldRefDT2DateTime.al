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
        MyDate: Date;
        MyTime: Time;
    begin
        MyTable.FindFirst();

        MyRecRef.GetTable(MyTable);
        MyFieldRef := MyRecRef.Field(1);

        MyDate := [|DT2Date(MyFieldRef.Value)|];
        MyTime := [|DT2Time(MyFieldRef.Value)|];
    end;
}
