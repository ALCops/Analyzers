namespace MyPublisher.MyExtension.MyAppDomain;

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyPublisher.MyExtension.MyAppDomain.MyTable;
    begin
        [|MyTable.SetLoadFields(MyTable."No.")|];
        MyTable.Get('001');
        Reload(MyTable);
        MyTable.Delete();
    end;

    local procedure Reload(var Rec: Record MyPublisher.MyExtension.MyAppDomain.MyTable)
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
