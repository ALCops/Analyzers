codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetLoadFields("My Field Name");
        MyTable.Get();
        exit(MyTable."My Field Name");
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; "My Field Name"; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}
