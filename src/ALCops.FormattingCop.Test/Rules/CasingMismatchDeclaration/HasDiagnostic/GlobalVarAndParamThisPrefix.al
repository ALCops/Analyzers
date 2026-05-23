// Global var accessed via this. prefix but with wrong casing.
// this.myTable should be this.MyTable to match global var - diagnostic expected.
// Regression test for https://github.com/ALCops/Analyzers/issues/307

codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure(myTable: Record MyTable)
    begin
        if [|this.myTable|]."No." = '' then
            exit;
    end;
}

table 50105 MyTable { fields { field(1; "No."; Code[20]) { } } }
