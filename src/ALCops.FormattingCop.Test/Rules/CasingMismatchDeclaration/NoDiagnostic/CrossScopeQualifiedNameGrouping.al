// Tests that cross-scope qualified names do NOT produce false positives
// when different methods use different parameter casing.
// Regression test for https://github.com/ALCops/Analyzers/issues/307

codeunit 50100 MyCodeunit
{
    procedure MyProcedure1(PurchaseHeader: Record PurchaseHeader)
    begin
        if [|PurchaseHeader|]."No." = '' then
            exit;
    end;

    procedure MyProcedure2(purchaseHeader: Record PurchaseHeader)
    begin
        if [|purchaseHeader|]."No." = '' then
            exit;
    end;
}

table 50106 PurchaseHeader { fields { field(1; "No."; Code[20]) { } } }
