// Global var and local variable with same name, different casing.
// Usage in method uses global var casing instead of local var casing - diagnostic expected.
// Regression test for https://github.com/ALCops/Analyzers/issues/307

codeunit 50100 MyCodeunit
{
    var
        MyTable: Record MyTable;

    procedure MyProcedure()
    var
        myTable: Record MyTable;
    begin
        if [|MyTable|]."No." = '' then
            exit;
    end;
}

table 50105 MyTable { fields { field(1; "No."; Code[20]) { } } }
