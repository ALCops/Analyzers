codeunit 50100 "FormattingCop Test"
{
    procedure TestParentheses()
    var
        Customer: Record Customer;
    begin
        [|Customer.Reset|];
        Customer.SetCurrentKey("No.");
        [|Customer.FindFirst|];
        [|Customer.Next|];
        [|Customer.Reset|];
    end;
}
/*EOD*/
codeunit 50101 "FormattingCop Test Vendor"
{
    procedure TestParentheses()
    var
        Vendor: Record Vendor;
    begin
        [|Vendor.Reset|];
        Vendor.SetCurrentKey("No.");
        [|Vendor.FindFirst|];
        [|Vendor.Next|];
        [|Vendor.Reset|];
    end;
}

table 50100 Customer
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }
}

table 50101 Vendor
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }
}