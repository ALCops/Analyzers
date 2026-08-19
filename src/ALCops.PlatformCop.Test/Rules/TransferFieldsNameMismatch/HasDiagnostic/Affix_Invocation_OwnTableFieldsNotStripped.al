table 50100 MyTableA
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        [|field(50100; "Customer Name"; Text[100]) { }|]
    }
}

table 50101 MyTableB
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        [|field(50100; "ABC Customer Name"; Text[100]) { }|] // Own (non-extension) table field: affix is NOT stripped
    }
}

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        FromRec: Record MyTableB;
        ToRec: Record MyTableA;
    begin
        [|ToRec.TransferFields(FromRec)|];
    end;
}
