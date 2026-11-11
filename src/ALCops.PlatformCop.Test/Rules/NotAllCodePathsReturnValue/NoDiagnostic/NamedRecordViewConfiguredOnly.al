table 50100 MyTable
{
    fields
    {
        field(1; "No."; Code[20]) { }
        field(2; Description; Text[100]) { }
    }

    keys
    {
        key(PK; "No.") { Clustered = true; }
    }
}
codeunit 50101 MyCodeunit
{
    procedure [|GetLines|](No: Code[20]) MyRec: Record MyTable
    begin
        MyRec.SetCurrentKey(Description);
        MyRec.SetLoadFields(Description);
        MyRec.FilterGroup(2);
        MyRec.SetRange("No.", No);
        MyRec.FilterGroup(0);
        MyRec.SetFilter(Description, '<>%1', '');
    end;
}
