table 50100 MyTableA
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
        field(50100; "Customer Name"; Text[100]) { }
    }
}

table 50101 MyTableB
{
    fields
    {
        field(1; "Primary Key"; Code[20]) { }
    }
}

tableextension 50100 MyTableBExt extends MyTableB
{
    fields
    {
        [|field(50100; "ABC Customer Name"; Text[100]) { }|] // Mandatory prefix "ABC " stripped before comparison
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
