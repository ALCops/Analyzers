codeunit 50000 MyCodeunit
{

    trigger OnRun()
    var
        MyTable: Record MyTable;
    begin
        // read
        [|MyTable.Find();|]
        [|MyTable.FindFirst();|]
        [|MyTable.FindLast();|]
        [|MyTable.FindSet();|]
        [|MyTable.Get(1);|]
        [|if MyTable.IsEmpty() then;|]

        // insert
        [|MyTable.Insert();|]

        // modify
        [|MyTable.Modify();|]
        [|MyTable.ModifyAll(MyField2, 2);|]
        [|MyTable.Rename(1);|]

        // delete
        [|MyTable.Delete();|]
        [|MyTable.DeleteAll();|]
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
        field(2; MyField2; Integer)
        {
            Caption = '', Locked = true;
            DataClassification = ToBeClassified;
        }
    }
}