codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MyTable: Record MyTable;
        Line: Text;
        Result: Text;
    begin
        foreach Line in GetLines([|MyTable.FindSet()|]) do
            Message(Line);
        repeat
            Result += MyTable.MyField;
        until MyTable.Next() = 0;
        exit(Result);
    end;

    local procedure GetLines(Found: Boolean): List of [Text]
    var
        Lines: List of [Text];
    begin
        if Found then
            Lines.Add('Found');
        exit(Lines);
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
