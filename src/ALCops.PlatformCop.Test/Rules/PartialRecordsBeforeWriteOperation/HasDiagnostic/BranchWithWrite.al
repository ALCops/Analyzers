codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Condition: Boolean)
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.SetLoadFields(MyTable."No.")|];
        MyTable.Get('001');
        if Condition then
            MyTable.Modify();
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Description; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { Clustered = true; }
    }
}
