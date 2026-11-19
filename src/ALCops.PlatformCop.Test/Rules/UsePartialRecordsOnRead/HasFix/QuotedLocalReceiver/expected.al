codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        "My Table": Record "My Table";
    begin
        "my table".SetLoadFields(Description);
        "my table".Get();
        exit("my table".Description);
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
