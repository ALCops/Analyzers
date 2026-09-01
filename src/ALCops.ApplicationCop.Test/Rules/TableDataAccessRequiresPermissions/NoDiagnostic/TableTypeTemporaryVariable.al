codeunit 50000 MyCodeunit
{

    trigger OnRun()
    var
        MyTempTable: Record MyTempTable;
    begin
        [|MyTempTable.Insert();|]
        [|MyTempTable.Modify();|]
        [|MyTempTable.Find();|]
        [|MyTempTable.FindFirst();|]
        [|if MyTempTable.IsEmpty() then;|]
        [|MyTempTable.Delete();|]
        [|MyTempTable.DeleteAll();|]
    end;
}

table 50000 MyTempTable
{
    Caption = '', Locked = true;
    TableType = Temporary;

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
