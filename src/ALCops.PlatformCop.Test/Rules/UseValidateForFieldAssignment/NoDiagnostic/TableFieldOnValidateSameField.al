table 50100 "Sales Line"
{
    fields
    {
        field(1; "Unit Price"; Decimal)
        {
            trigger OnValidate()
            begin
                Rec.[|"Unit Price"|] := Round(Rec."Unit Price", 0.01);
            end;
        }
        field(2; Quantity; Decimal) { }
    }
}
