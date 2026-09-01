page 50100 MyPage
{
    actions
    {
        area(Processing)
        {
            action(Import)
            {
                trigger OnAction()
                begin
                    Xmlport.Run([|Xmlport|]::"My Xmlport");
                end;
            }
        }
    }
}

codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        Xmlport.Run([|Xmlport|]::"My Xmlport");
        Xmlport.Run([|Xmlport|]::"My Xmlport", true);
    end;
}

xmlport 50100 "My Xmlport" { schema { textelement(NodeName1) { } } }
