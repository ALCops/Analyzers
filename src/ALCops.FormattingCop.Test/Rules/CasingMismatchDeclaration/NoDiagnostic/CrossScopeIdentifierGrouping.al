// Tests that same-text identifiers in different methods with different parameter casing
// do NOT produce false positives on the correctly-cased usage.
// Regression test for https://github.com/ALCops/Analyzers/issues/307

codeunit 50100 MyCodeunit
{
    procedure MyProcedure1(MyTable: Record MyTable)
    begin
        if [|MyTable|]."No." = '' then
            exit;
    end;

    procedure MyProcedure2(myTable: Record MyTable)
    begin
        if [|myTable|]."No." = '' then
            exit;
    end;
}

table 50105 MyTable { fields { field(1; "No."; Code[20]) { } } }
