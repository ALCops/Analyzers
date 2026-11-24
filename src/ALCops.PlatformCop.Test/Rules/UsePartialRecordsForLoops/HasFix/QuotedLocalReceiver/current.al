codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        "My Table": Record "My Table";
        Result: Text;
    begin
        [|"my table".FindSet()|];
        repeat
            Result += "my table".Description;
        until "my table".Next() = 0;
        exit(Result);
    end;
}

table 50100 "My Table"
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Description; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}
