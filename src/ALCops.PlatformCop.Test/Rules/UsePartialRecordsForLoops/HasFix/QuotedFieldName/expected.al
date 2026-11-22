codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
        Result: Text;
    begin
        MyTable.SetLoadFields("My Field Name");
        MyTable.FindSet();
        repeat
            Result += MyTable."My Field Name";
        until MyTable.Next() = 0;
        exit(Result);
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
