interface TaxTotalsProvider
{
    procedure GetTaxCategory(TaxCode: Code[10]): Text;

    procedure FinalizeTaxTotals(var [|TaxLine: Record TaxLine|])
    begin
    end;
}

table 50100 TaxLine
{
    fields
    {
        field(1; "Entry No."; Integer) { }
    }
}
