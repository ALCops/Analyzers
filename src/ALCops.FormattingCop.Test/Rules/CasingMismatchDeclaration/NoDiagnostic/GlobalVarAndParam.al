// Global var and parameter with same name, different casing.
// Parameter usage correctly cased - no diagnostic expected.
// Regression test for https://github.com/ALCops/Analyzers/issues/307

codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure(myTable: Record MyTable)
    begin
        if [|myTable|]."No." = '' then
            exit;
    end;
}

table 50105 MyTable { fields { field(1; "No."; Code[20]) { } } }
