table 50100 "Sales Line"
{
    fields
    {
        field(1; "Unit Price"; Decimal)
        {
            trigger OnValidate()
            begin
                Rec.[|"Line Amount"|] := Rec."Unit Price" * Rec.Quantity;
            end;
        }
        field(2; Quantity; Decimal) { }
        field(3; "Line Amount"; Decimal) { }
    }
}
