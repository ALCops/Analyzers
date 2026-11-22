interface MyInterface
{
    procedure AbstractMethod();

    procedure [|DefaultMethod|]() // Cognitive Complexity: 15 (threshold >=15)
    var
        Counter: Integer;
    begin
        if Counter = 1 then                             // +1 (nesting = 0)
            if Counter = 2 then                         // +2 (nesting = 1)
                if Counter = 3 then                     // +3 (nesting = 2)
                    if Counter = 4 then                 // +4 (nesting = 3)
                        if Counter = 5 then             // +5 (nesting = 4)
                            Counter += 1;
    end;
}
