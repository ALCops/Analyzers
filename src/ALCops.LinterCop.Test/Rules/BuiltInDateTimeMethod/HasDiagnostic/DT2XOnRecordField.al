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
        MyDate: Date;
        MyTime: Time;
    begin
        MyTable.FindFirst();

        MyDate := [|DT2Date(MyTable."My DateTime")|];
        MyTime := [|DT2Time(MyTable."My DateTime")|];
    end;
}
