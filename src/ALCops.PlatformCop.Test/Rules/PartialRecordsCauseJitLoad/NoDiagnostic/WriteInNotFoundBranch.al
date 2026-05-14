codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.SetLoadFields(MyTable."No.")|];
        MyTable.SetRange("No.", '001');
        if not MyTable.FindFirst() then begin
            MyTable.Init();
            MyTable.Insert(true);
        end;
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
