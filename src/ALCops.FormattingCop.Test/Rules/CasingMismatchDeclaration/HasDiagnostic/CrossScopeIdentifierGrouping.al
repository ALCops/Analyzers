// Tests that the diagnostic IS still raised on incorrectly-cased usage
// when a method uses the wrong casing for its own parameter.
// Regression test for https://github.com/ALCops/Analyzers/issues/307

codeunit 50100 MyCodeunit
{
    procedure MyProcedure1(MyTable: Record MyTable)
    begin
        if MyTable."No." = '' then
            exit;
    end;

    procedure MyProcedure2(myTable: Record MyTable)
    begin
        if [|MyTable|]."No." = '' then
            exit;
    end;
}

table 50105 MyTable { fields { field(1; "No."; Code[20]) { } } }
