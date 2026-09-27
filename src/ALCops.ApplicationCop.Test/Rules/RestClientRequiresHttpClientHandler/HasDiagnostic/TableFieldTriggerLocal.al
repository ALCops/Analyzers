table 50100 [|MyTable|]
{
    fields
    {
        field(1; MyField; Integer)
        {
            trigger OnValidate()
            var
                RestClient: Codeunit "Rest Client";
            begin
            end;
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
