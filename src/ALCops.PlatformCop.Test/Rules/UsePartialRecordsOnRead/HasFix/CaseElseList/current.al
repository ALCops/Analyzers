codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Kind: Integer; No: Code[20]): Text
    var
        MyTable: Record MyTable;
    begin
        case Kind of
            1:
                exit('');
            else
                Kind := 0;
                [|MyTable.Get(No)|];
        end;
        exit(MyTable.MyField);
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