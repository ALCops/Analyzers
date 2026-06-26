table 50100 "Sales Line"
{
    fields
    {
        field(1; "Unit Price"; Decimal) { }
    }
}

page 50100 "Sales Line Card"
{
    PageType = Card;
    SourceTable = "Sales Line";

    layout
    {
        area(Content)
        {
            field(UnitPrice; Rec."Unit Price")
            {
                trigger OnValidate()
                begin
                    [|"Unit Price"|] := 100;
                end;
            }
        }
    }
}
