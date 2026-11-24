codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Found: Boolean): Text
    var
        MyTable: Record MyTable;
        OtherTable: Record MyTable;
    begin
        if Found then
            if [|MyTable.FindSet()|] then
                while MyTable.Next() <> 0 do
                    if [|OtherTable.FindSet()|] then
                        repeat
                            Message(OtherTable.MyField);
                        until OtherTable.Next() = 0;
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