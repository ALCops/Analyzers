codeunit 50100 RecordTypeNameRequiresQuotes
{
    var
        [|SalesHeader|]: Record "Sales Header";

    local procedure JustATest()
    begin
        SalesHeader.Get('10000');
        Message('%1', SalesHeader."No.");
    end;
}

table 50101 "Sales Header"
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }

    keys
    {
        key(PK; "No.") { Clustered = true; }
    }
}
