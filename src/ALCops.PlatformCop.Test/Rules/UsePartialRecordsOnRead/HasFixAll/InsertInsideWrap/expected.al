codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Found: Boolean; No: Code[20]): Text
    var
        MyTable: Record MyTable;
        OtherTable: Record MyTable;
    begin
        if Found then begin
            MyTable.SetLoadFields("No.");
            if MyTable.Get(No) then begin
                OtherTable.SetLoadFields(MyField);
                OtherTable.Get(MyTable."No.");
                exit(OtherTable.MyField);
            end;
        end;
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}