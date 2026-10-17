codeunit 50100 MyCodeunit
{
    procedure DoSomething()
    var
        [|RestClient|]: Codeunit "Rest Client";
    begin
        [|RestClient.Initialize()|];
        RestClient.Get('https://example.com');
    end;
}

codeunit 50104 "Rest Client"
{
    procedure Initialize()
    begin
    end;

    procedure Get(RequestUri: Text)
    begin
    end;
}
