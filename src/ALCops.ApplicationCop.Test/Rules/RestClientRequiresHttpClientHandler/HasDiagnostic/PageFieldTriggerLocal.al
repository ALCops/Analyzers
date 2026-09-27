page 50100 [|MyPage|]
{
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            field(MyField; Rec.MyField)
            {
                trigger OnValidate()
                var
                    RestClient: Codeunit "Rest Client";
                begin
                end;
            }
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
