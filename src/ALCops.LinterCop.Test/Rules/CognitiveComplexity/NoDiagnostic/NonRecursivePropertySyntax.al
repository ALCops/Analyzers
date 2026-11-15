codeunit 50100 MyCodeunit
{
    var
        Counter: Integer;

    procedure [|MyProcedure|](Value: Integer)  // Cognitive Complexity: 14 (threshold >=15); a setter call miscounted as recursion would reach 15
    var
        OtherCodeunit: Codeunit OtherCodeunit;
        Condition: Boolean;
    begin
        if true then                            // +1 (nesting = 0)
            if true then                        // +2 (nesting = 1)
                OtherCodeunit.Relay := Value;   // +0 (named-receiver setter call, Relay does not call back)

        while true do                           // +1 (nesting = 0)
            SetCounter := Value;                // +0 (bare setter call, SetCounter does not call back)

        if true then                            // +1 (nesting = 0)
            if Condition then                   // +2 (nesting = 1)
                Counter := Value                // +0 (ordinary assignment)
            else                                // +2 (nesting = 1)
                OtherCodeunit.Relay := Value;   // +0 (named-receiver setter call)

        repeat                                  // +1 (nesting = 0)
            SetCounter := Value;                // +0 (bare setter call)
        until true;

        while true do                           // +1 (nesting = 0)
            if true then                        // +2 (nesting = 1)
                Counter := Value;               // +0 (ordinary assignment)

        if true then                            // +1 (nesting = 0)
            OtherCodeunit.Relay := Value;       // +0 (named-receiver setter call)
    end;

    procedure SetCounter(NewValue: Integer)
    begin
        Counter := NewValue;
    end;
}

codeunit 50101 OtherCodeunit
{
    var
        LastValue: Integer;

    procedure Relay(Value: Integer)
    begin
        LastValue := Value;
    end;
}
