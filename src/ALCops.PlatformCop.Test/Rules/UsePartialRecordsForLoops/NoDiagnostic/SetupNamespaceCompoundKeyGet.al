namespace MyPublisher.MyExtension.Setup;

codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Text
    var
        MySetup: Record MySetup;
        Result: Text;
    begin
        [|MySetup.FindSet()|];
        repeat
            Result += Format(MySetup.MyField);
        until MySetup.Next() = 0;
        exit(Result);
    end;
}

table 50100 MySetup
{
    fields
    {
        field(1; "Gen. Bus. Posting Group"; Code[20]) { }
        field(2; "Gen. Prod. Posting Group"; Code[20]) { }
        field(3; MyField; Text[100]) { }
    }

    keys
    {
        key(PK; "Gen. Bus. Posting Group", "Gen. Prod. Posting Group") { }
    }
}
