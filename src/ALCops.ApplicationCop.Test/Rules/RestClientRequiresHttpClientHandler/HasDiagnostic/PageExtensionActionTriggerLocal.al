pageextension 50100 [|MyPageExtension|] extends MyPage
{
    actions
    {
        addlast(Processing)
        {
            action(MyNewAction)
            {
                trigger OnAction()
                var
                    RestClient: Codeunit "Rest Client";
                begin
                end;
            }
        }
    }
}

page 50100 MyPage
{
    SourceTable = MyTable;

    actions
    {
        area(Processing)
        {
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Integer)
        {
        }
    }
}

codeunit 2350 "Rest Client"
{
}

interface "Http Client Handler"
{
    procedure Send(CurrHttpClientInstance: HttpClient; HttpRequestMessage: HttpRequestMessage; var HttpResponseMessage: HttpResponseMessage): Boolean;
}
