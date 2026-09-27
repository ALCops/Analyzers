tableextension 50100 [|MyTableExtension|] extends MyTable
{
    fields
    {
        modify(MyField)
        {
            trigger OnAfterValidate()
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
