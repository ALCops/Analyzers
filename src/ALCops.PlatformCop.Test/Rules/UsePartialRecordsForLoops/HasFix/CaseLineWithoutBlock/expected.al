codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Kind: Integer): Text
    var
        MyTable: Record MyTable;
    begin
        case Kind of
            1:
                begin
                    MyTable.SetLoadFields(MyField);
                    if MyTable.FindSet() then
                        repeat
                            Message(MyTable.MyField);
                        until MyTable.Next() = 0;
                end;
            2:
                exit('');
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