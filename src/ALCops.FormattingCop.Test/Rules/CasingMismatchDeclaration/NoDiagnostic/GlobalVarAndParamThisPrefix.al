// Global var accessed via this. prefix while parameter has different casing.
// this.MyTable correctly references the global var - no diagnostic expected.
// Regression test for https://github.com/ALCops/Analyzers/issues/307

codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure(myTable: Record MyTable)
    begin
        if [|this.MyTable|]."No." = '' then
            exit;
    end;
}

table 50105 MyTable { fields { field(1; "No."; Code[20]) { } } }
