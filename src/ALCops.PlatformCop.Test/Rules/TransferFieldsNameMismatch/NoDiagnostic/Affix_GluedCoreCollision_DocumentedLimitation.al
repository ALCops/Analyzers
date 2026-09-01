tableextension 50100 MyCustomerExt extends Customer
{
    fields
    {
        // Accepted SDK-parity limitation (issue #436): PC0021 does NOT fire here.
        // The mandatory affix "MER" is matched case-insensitively with no word boundary
        // (mirroring the platform's RuleIdentifiersMustHaveValidAffixes.VerifyAffixIsUsed,
        // which uses StringComparison.OrdinalIgnoreCase). Both names are therefore validly
        // affixed and strip to the same core "Custo":
        //   "Customer" -> trailing "mer" stripped -> "Custo"
        //   "CustoMER" -> trailing "MER" stripped -> "Custo"
        // So the genuinely different field names are treated as equivalent and the mismatch
        // is intentionally suppressed. Hardening this (case-sensitive or word-boundary
        // stripping) was rejected because it would diverge from the platform and cause false
        // positives on legitimately glued affixes.
        [|field(50100; "Customer"; Integer) { }|]
    }
}

tableextension 50101 MyContactExt extends Contact
{
    fields
    {
        [|field(50100; "CustoMER"; Integer) { }|]
    }
}

table 18 Customer { fields { field(1; "No."; Code[20]) { } } }
table 5050 Contact { fields { field(1; "No."; Code[20]) { } } }
