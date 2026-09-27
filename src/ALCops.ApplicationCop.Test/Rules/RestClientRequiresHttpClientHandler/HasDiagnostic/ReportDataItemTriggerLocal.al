report 50100 [|MyReport|]
{
    ProcessingOnly = true;

    dataset
    {
        dataitem(MyTable; MyTable)
        {
            trigger OnAfterGetRecord()
            var
                RestClient: Codeunit "Rest Client";
            begin
            end;
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
