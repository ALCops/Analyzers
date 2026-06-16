codeunit 50000 MyCodeunit
{

    trigger OnRun()
    var
        MyTable: Record "ABC Example Header.Line";
    begin
        [|MyTable.FindFirst();|]
    end;
}

table 50000 "ABC Example Header.Line"
{
    fields
    {
        field(1; MyField; Integer) { }
    }
}
