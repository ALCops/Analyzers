codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.SetLoadFields(MyTable."No.")|];
        MyTable.SetRange("No.", '001', '999');
        MyTable.FindSet();
        repeat
            MyTable.Delete();
        until MyTable.Next() = 0;
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
