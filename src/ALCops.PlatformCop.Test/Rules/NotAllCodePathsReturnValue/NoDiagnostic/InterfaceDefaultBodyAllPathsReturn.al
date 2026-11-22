interface "Shipping Provider"
{
    procedure GetRate(Weight: Decimal): Decimal;

    procedure [|GetTrackingUrl|](TrackingNo: Text): Text
    begin
        if TrackingNo = '' then
            exit('');
        exit('https://example.com/track/' + TrackingNo);
    end;

    procedure [|SupportsTracking|](): Boolean
    begin
        exit(false);
    end;
}