codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        TempMyTable: Record MyTable temporary;
    begin
        [|MyTable.SetLoadFields(MyTable."No.")|];
        TempMyTable := MyTable;
        MyTable.Get('001');
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
