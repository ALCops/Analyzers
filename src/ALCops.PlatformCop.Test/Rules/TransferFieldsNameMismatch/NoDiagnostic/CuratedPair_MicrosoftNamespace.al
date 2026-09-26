// Tracking Specification -> Reservation Entry is a curated TransferFields relation and both
// tables live in the Microsoft namespace. Fields 31 and 900 differ by name in the Base App;
// the developer cannot rename them, so neither the call nor the fields are reported.
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
        [|field(900; "Disallow Cancellation"; Boolean) { }|]
    }
}
