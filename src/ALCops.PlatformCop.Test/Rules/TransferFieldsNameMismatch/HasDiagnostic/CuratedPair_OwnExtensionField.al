// Curated Microsoft pair: the Microsoft-owned differences on fields 31 and 900 are not
// reported, but fields added by tableextensions of the analyzed module are always checked,
// so the collision on field 50100 is still reported at the call.
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
        field(31; "Qty. Rounding Precision (Base)"; Decimal) { }
        field(900; "Prohibit Cancellation"; Boolean) { }
    }
}

table 337 "Reservation Entry"
{
    fields
    {
        field(1; "Entry No."; Integer) { }
        field(31; "Action Message Adjustment"; Decimal) { }
        field(900; "Disallow Cancellation"; Boolean) { }
    }
}

tableextension 50100 MyTrackingSpecExt extends "Tracking Specification"
{
    fields
    {
        field(50100; "My Field A"; Integer) { }
    }
}

tableextension 50101 MyReservationEntryExt extends "Reservation Entry"
{
    fields
    {
        field(50100; "My Field B"; Integer) { } // Same ID (50100) as in MyTrackingSpecExt, different name
    }
}
