codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Reloaded: Boolean)
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.SetLoadFields(MyTable."No.")|];
        MyTable.Get('001');
        if Reloaded then
            Reload(MyTable);
        MyTable.Delete();
    end;

    local procedure Reload(var Rec: Record MyTable)
    begin
        Rec.SetLoadFields();
        Rec.Get('001');
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
