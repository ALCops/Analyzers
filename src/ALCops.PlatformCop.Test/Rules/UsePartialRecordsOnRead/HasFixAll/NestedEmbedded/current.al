codeunit 50100 MyCodeunit
{
    procedure IsSepa(var PaymentLine: Record MyPaymentLine) Result: Boolean
    var
        CustBank: Record MyCustomerBankAccount;
        VendBank: Record MyVendorBankAccount;
        Customer: Record MyCustomer;
        Vendor: Record MyVendor;
        Country: Record MyCountry;
    begin
        if PaymentLine.FindSet() then begin
            Result := true;
            repeat
                case PaymentLine.Type of
                    PaymentLine.Type::Customer:
                        if [|CustBank.Get(PaymentLine."No.", PaymentLine."Bank Account Code")|] then begin
                            Country.SetLoadFields("EU Country/Region Code");
                            if Country.Get(CustBank."Country/Region Code") then begin
                                if Country."EU Country/Region Code" = '' then
                                    Result := false;
                            end else
                                Result := false;
                        end else
                            if [|Customer.Get(PaymentLine."No.")|] then
                                if [|Country.Get(Customer."Country/Region Code")|] then begin
                                    if Country."EU Country/Region Code" = '' then
                                        Result := false;
                                end else
                                    Result := false;
                    PaymentLine.Type::Vendor:
                        if [|VendBank.Get(PaymentLine."No.", PaymentLine."Bank Account Code")|] then begin
                            Country.SetLoadFields("EU Country/Region Code");
                            if Country.Get(VendBank."Country/Region Code") then begin
                                if Country."EU Country/Region Code" = '' then
                                    Result := false;
                            end else
                                Result := false;
                        end else
                            if [|Vendor.Get(PaymentLine."No.")|] then
                                if [|Country.Get(Vendor."Country/Region Code")|] then begin
                                    if Country."EU Country/Region Code" = '' then
                                        Result := false;
                                end else
                                    Result := false;
                end;
            until PaymentLine.Next() = 0;
        end;
    end;
}

table 50100 MyPaymentLine
{
    fields
    {
        field(1; "Line No."; Integer) { }
        field(2; Type; Option) { OptionMembers = Customer,Vendor; }
        field(3; "No."; Code[20]) { }
        field(4; "Bank Account Code"; Code[20]) { }
    }
}

table 50101 MyCustomerBankAccount
{
    fields
    {
        field(1; "Customer No."; Code[20]) { }
        field(2; "Code"; Code[20]) { }
        field(3; "Country/Region Code"; Code[10]) { }
    }

    keys
    {
        key(PK; "Customer No.", "Code") { }
    }
}

table 50102 MyVendorBankAccount
{
    fields
    {
        field(1; "Vendor No."; Code[20]) { }
        field(2; "Code"; Code[20]) { }
        field(3; "Country/Region Code"; Code[10]) { }
    }

    keys
    {
        key(PK; "Vendor No.", "Code") { }
    }
}

table 50103 MyCustomer
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; "Country/Region Code"; Code[10]) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}

table 50104 MyVendor
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; "Country/Region Code"; Code[10]) { }
    }

    keys
    {
        key(PK; "No.") { }
    }
}

table 50105 MyCountry
{
    fields
    {
        field(1; "Country No."; Code[10]) { }
        field(2; "EU Country/Region Code"; Code[10]) { }
    }

    keys
    {
        key(PK; "Country No.") { }
    }
}