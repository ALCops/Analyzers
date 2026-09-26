// Bank Deposit Header -> Posted Bank Deposit Header is curated in that direction only. This call
// runs the other way (source = Posted Bank Deposit Header, target = Bank Deposit Header), so it is
// not a supported pair: the mismatch is reported at the call and on both fields, although both
// tables are Microsoft's.
namespace Microsoft.Bank.Deposit;

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        BankDepositHeader: Record "Bank Deposit Header";
        PostedBankDepositHeader: Record "Posted Bank Deposit Header";
    begin
        [|BankDepositHeader.TransferFields(PostedBankDepositHeader)|];
    end;
}

table 1690 "Bank Deposit Header"
{
    fields
    {
        field(1; "No."; Code[20]) { }
        [|field(2; "Bank Account No."; Code[20]) { }|]
    }
}

table 1695 "Posted Bank Deposit Header"
{
    fields
    {
        field(1; "No."; Code[20]) { }
        [|field(2; "Posted Bank Account No."; Code[20]) { }|] // Same ID (2) as in Bank Deposit Header, different name
    }
}
