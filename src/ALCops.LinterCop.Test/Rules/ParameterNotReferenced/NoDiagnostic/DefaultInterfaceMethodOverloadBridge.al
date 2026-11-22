interface ExemptionReasonProvider
{
    procedure GetExemptionReason(var ReasonText: Text; CategoryId: Text);

    procedure GetExemptionReason([|TaxLine: Record TaxLine|]; var ReasonText: Text; CategoryId: Text)
    begin
        this.GetExemptionReason(ReasonText, CategoryId);
    end;
}

table 50100 TaxLine
{
    fields
    {
        field(1; "Entry No."; Integer) { }
    }
}
