table 50100 "Sales Line"
{
    fields
    {
        field(1; "Unit Price"; Decimal) { }
    }

    procedure DoSomething()
    begin
        this.[|"Unit Price"|] := 100;
    end;
}
