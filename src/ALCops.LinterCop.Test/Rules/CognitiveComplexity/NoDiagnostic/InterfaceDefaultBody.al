interface MyInterface
{
    procedure [|AbstractMethod|]();

    procedure [|DefaultMethod|]() // Cognitive Complexity: 14 (threshold >=15)
    var
        Counter: Integer;
    begin
        if Counter = 1 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 2 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 3 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 4 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 5 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 6 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 7 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 8 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 9 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 10 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 11 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 12 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 13 then // +1 (nesting = 0)
            Counter += 1;
        if Counter = 14 then // +1 (nesting = 0)
            Counter += 1;
    end;
}
