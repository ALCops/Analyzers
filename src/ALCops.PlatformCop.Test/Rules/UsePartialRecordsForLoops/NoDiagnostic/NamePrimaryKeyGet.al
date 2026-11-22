codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
        Result: Text;
    begin
        [|MyTable.FindSet()|];
        repeat
            Result += Format(MyTable.Description);
        until MyTable.Next() = 0;
        exit(Result);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; Name; Code[10]) { }
        field(2; Description; Text[100]) { }
    }

    keys
    {
        key(PK; Name) { }
    }
}
