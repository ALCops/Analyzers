codeunit 50100 MyCodeunit
{
    var
        MyTable: Record [|"My Customer"|];
        MyInterface: Interface [|IMyInterface|];
        MyCodeunit2: Codeunit [|MyHelper|];
        MyPage: Page [|"My Customer Card"|];
        MyXmlPort: XmlPort [|"My Export"|];
        MyInterfaceList: List of [Interface [|IMyInterface|]];
        MyTableById: Record [|50100|];

    procedure MyProcedure(ParamTable: Record [|"My Customer"|]) ReturnTable: Record [|"My Customer"|]
    var
        LocalTable: Record [|"My Customer"|];
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
