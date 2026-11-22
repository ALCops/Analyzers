interface "Shipping Provider"
{
    procedure GetRate(Weight: Decimal): Decimal;

    procedure [|SupportsTracking|](): Boolean
    begin
    end;
}