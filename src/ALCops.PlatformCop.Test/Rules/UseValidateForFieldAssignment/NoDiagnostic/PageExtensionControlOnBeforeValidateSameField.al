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
            field(UnitPrice; Rec."Unit Price") { }
        }
    }
}

pageextension 50101 "Sales Line Card Ext" extends "Sales Line Card"
{
    layout
    {
        modify(UnitPrice)
        {
            trigger OnBeforeValidate()
            begin
                Rec.[|"Unit Price"|] := 100;
            end;
        }
    }
}
