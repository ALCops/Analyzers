table 50100 "Sales Line"
{
    fields
    {
        field(1; "Unit Price"; Decimal)
        {
            trigger OnValidate()
            var
                OtherSalesLine: Record "Sales Line";
            begin
                OtherSalesLine.[|"Unit Price"|] := 100;
            end;
        }
    }
}
