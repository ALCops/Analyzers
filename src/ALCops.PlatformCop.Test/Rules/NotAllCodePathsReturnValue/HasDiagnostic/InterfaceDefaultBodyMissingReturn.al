interface "Shipping Provider"
{
    procedure GetRate(Weight: Decimal): Decimal;

    procedure [|GetTrackingUrl|](TrackingNo: Text): Text
    begin
        if TrackingNo <> '' then
            exit('https://example.com/track/' + TrackingNo);
    end;
}