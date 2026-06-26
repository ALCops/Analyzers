table 50100 "Sales Line"
{
    fields
    {
        field(1; "Unit Price"; Decimal)
        {
            trigger OnValidate()
            begin
                xRec.[|"Unit Price"|] := 100;
            end;
        }
    }
}
