// The stored relation is Microsoft.Sales.History/"Sales Cr.Memo Header" ->
// Microsoft.EServices.EDocument/"E-Invoice Export Header". Here the namespace is declared
// with different literal casing ("microsoft.sales.history"). AL namespaces are
// case-insensitive, so the relation must still match and the diagnostic must still fire.
namespace microsoft.sales.history;

table 50140 "Sales Cr.Memo Header"
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }
}

table 50141 "E-Invoice Export Header"
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }
}

tableextension 50142 MyCrMemoExt extends "Sales Cr.Memo Header"
{
    fields
    {
        [|field(50100; MyFieldA; Integer) { }|]
    }
}

tableextension 50143 MyEInvExt extends "E-Invoice Export Header"
{
    fields
    {
        [|field(50100; MyFieldB; Integer) { }|] // Same ID (50100) as in MyCrMemoExt, different name
    }
}
