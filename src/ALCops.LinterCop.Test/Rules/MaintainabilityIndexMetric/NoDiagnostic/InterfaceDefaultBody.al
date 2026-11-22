interface MyInterface
{
    procedure [|AbstractMethod()|];

    procedure [|DefaultMethod()|]
    var
        Counter: Integer;
    begin
        if Counter = 1 then
            Counter += 1;
        if Counter = 2 then
            Counter += 1;
        if Counter = 3 then
            Counter += 1;
        if Counter = 4 then
            Counter += 1;
        if Counter = 5 then
            Counter += 1;
        if Counter = 6 then
            Counter += 1;
    end;
}