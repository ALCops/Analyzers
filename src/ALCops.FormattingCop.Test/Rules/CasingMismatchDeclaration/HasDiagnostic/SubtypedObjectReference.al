codeunit 50100 MyCodeunit
{
    var
        MyTable: Record [|"MY CUSTOMER"|];
        MyInterface: Interface [|"IMYINTERFACE"|];
        MyCodeunit2: Codeunit [|"MYHELPER"|];
        MyPage: Page [|"MY CUSTOMER CARD"|];
        MyXmlPort: XmlPort [|"MY EXPORT"|];
        MyInterfaceList: List of [Interface [|"IMYINTERFACE"|]];

    procedure MyProcedure(ParamTable: Record [|"MY CUSTOMER"|]) ReturnTable: Record [|"MY CUSTOMER"|]
    var
        LocalTable: Record [|"MY CUSTOMER"|];
    begin
    end;
}

table 50100 "My Customer"
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
    }
}

interface IMyInterface { }
codeunit 50101 MyHelper { }

page 50100 "My Customer Card"
{
    SourceTable = "My Customer";
}

xmlport 50100 "My Export"
{
    schema
    {
        textelement(Root)
        {
        }
    }
}
