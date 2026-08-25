codeunit 50100 MyCodeunit
{
    procedure [|MyProcedure|]() // Cognitive Complexity: 15 (threshold >=15)
    var
        Handler: Codeunit MyHandler;
    begin
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
        if true then Handler.Error(); // +1 (nesting = 0)
    end;
}

codeunit 50101 MyHandler
{
    procedure Error()
    begin
    end;
}