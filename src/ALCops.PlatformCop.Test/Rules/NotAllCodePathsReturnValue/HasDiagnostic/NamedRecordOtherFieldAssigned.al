table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
    }
}

codeunit 50101 MyCodeunit
{
    procedure [|Build|]() Result: Record MyTable
    var
        Other: Record MyTable;
    begin
        Other."No." := 'A';
    end;
}
