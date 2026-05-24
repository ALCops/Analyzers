xmlport 50100 MyXmlport
{
    schema
    {
        textelement(root)
        {
            textelement(MyTextelement) { }
        }
    }

    trigger OnPreXmlPort()
    begin
        MyTable.Get([|MyTextelement|]);
    end;

    var
        MyTable: Record MyTable;
}

table 50100 MyTable { fields { field(1; "No."; Code[20]) { } } }
