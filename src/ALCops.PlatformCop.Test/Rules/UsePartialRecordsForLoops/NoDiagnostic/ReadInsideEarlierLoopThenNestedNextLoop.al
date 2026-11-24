codeunit 50100 MyCodeunit
{
    procedure MyProcedure(Foo: Boolean): Text
    var
        MyTable: Record MyTable;
        i: Integer;
        Result: Text;
    begin
        for i := 1 to 3 do
            [|MyTable.Get(i)|];
        Result := MyTable.MyField;
        while Foo do
            while MyTable.Next() <> 0 do begin
                Result += MyTable.MyField;
                Foo := StrLen(Result) < 100;
            end;
        exit(Result);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        field(2; Parent; Integer) { }
        field(3; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Entry No.") { }
    }
}
