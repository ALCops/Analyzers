table 50100 "Sales Line"
{
    fields
    {
        field(1; "Unit Price"; Decimal) { }
    }
}

tableextension 50101 "Sales Line Ext" extends "Sales Line"
{
    fields
    {
        modify("Unit Price")
        {
            trigger OnBeforeValidate()
            begin
                Rec.[|"Unit Price"|] := 100;
            end;
        }
    }
}
