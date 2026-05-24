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
        MyTable.Validate(MyField, [|MyTextelement|]);
    end;

    var
        MyTable: Record MyTable;
}

table 50100 MyTable { fields { field(1; MyField; Code[20]) { } } }
