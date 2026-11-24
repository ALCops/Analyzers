codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
        Result: Text;
        Continue: Boolean;
    begin
        Continue := [|MyTable.FindSet()|];
        while Continue do begin
            Result += MyTable.MyField;
            Continue := StrLen(Result) < 100;
            MyTable.Next();
        end;
        exit(Result);
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
