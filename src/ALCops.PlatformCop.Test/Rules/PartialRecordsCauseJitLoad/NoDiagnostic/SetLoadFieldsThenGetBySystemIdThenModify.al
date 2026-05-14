codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.SetLoadFields(MyTable."No.")|];
        MyTable.GetBySystemId('00000000-0000-0000-0000-000000000001');
        MyTable.Description := 'Updated';
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
