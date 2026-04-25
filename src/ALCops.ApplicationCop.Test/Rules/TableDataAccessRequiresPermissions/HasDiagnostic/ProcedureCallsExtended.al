codeunit 50000 MyCodeunit
{

    trigger OnRun()
    var
        MyTable: Record MyTable;
    begin
        [|MyTable.Get(1);|]
        [|MyTable.FindFirst();|]
        [|MyTable.Insert();|]
        [|MyTable.Modify();|]
        [|MyTable.Delete();|]
        [|MyTable.GetBySystemId(MyTable.SystemId);|]
        [|if MyTable.Count() > 0 then;|]
    end;
}

table 50000 MyTable
{
    Caption = '', Locked = true;

    fields
    {
        field(1; MyField; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}
