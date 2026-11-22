interface "Shipping Provider"
{
    procedure [|GetRate|](Weight: Decimal): Decimal;

    procedure [|GetCarrierName|](): Text;
}