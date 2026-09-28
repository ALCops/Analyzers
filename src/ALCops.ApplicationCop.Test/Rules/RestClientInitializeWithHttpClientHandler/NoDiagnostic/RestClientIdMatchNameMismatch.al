codeunit 50100 MyCodeunit
{
    procedure DoSomething()
    var
        [|RestClient|]: Codeunit "My Client";
    begin
        [|RestClient.Initialize()|];
        RestClient.Get('https://example.com');
    end;
}

codeunit 2350 "My Client"
{
    procedure Initialize()
    begin
    end;

    procedure Get(RequestUri: Text)
    begin
    end;
}
