namespace MyPublisher.MyExtension.Setup;

codeunit 50100 MyCodeunit
{
    procedure MyProcedure(): Decimal
    var
        MySetup: Record MySetup;
        Total: Decimal;
    begin
        [|MySetup.FindSet()|];
        repeat
            Total += MySetup.Amount;
        until MySetup.Next() = 0;
        exit(Total);
    end;
}

table 50100 MySetup
{
    fields
    {
        field(1; "Gen. Bus. Posting Group"; Code[20]) { }
        field(2; "Gen. Prod. Posting Group"; Code[20]) { }
        field(3; Amount; Decimal) { }
    }

    keys
    {
        key(PK; "Gen. Bus. Posting Group", "Gen. Prod. Posting Group") { }
    }
}
