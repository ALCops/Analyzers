tableextension 50100 MyTableAExt extends MyTableA
{
    fields
    {
        [|field(50100; ExtField; Integer) { }|] // Same ID as MyTableB.ExtField in the dependency, different type
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
