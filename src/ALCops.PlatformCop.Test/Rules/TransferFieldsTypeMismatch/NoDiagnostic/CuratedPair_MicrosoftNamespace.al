// Tracking Specification -> Reservation Entry is a curated TransferFields relation and both
// tables live in the Microsoft namespace. The real pair differs by name only (fields 31 and
// 900); the type difference on field 900 is synthetic, to prove Microsoft-owned type
// mismatches on a supported pair are not reported either.
namespace Microsoft.Inventory.Tracking;

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        TrackingSpecification: Record "Tracking Specification";
        ReservationEntry: Record "Reservation Entry";
    begin
        [|ReservationEntry.TransferFields(TrackingSpecification)|];
    end;
}

table 336 "Tracking Specification"
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        [|field(31; "Qty. Rounding Precision (Base)"; Decimal) { }|]
        [|field(900; "Prohibit Cancellation"; Boolean) { }|]
    }
}

table 337 "Reservation Entry"
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        [|field(31; "Action Message Adjustment"; Decimal) { }|]
        [|field(900; "Disallow Cancellation"; Integer) { }|]
    }
}
