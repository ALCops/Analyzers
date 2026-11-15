codeunit 50100 MyCodeunit
{
    procedure [|CountDown|](Value: Integer)    // Cognitive Complexity: 15 (threshold >=15)
    var
        OtherCodeunit: Codeunit OtherCodeunit;
        Condition: Boolean;
    begin
        if true then                            // IfStatement: +1 (1 increment + 0 nesting penalty)
            if true then                        // IfStatement: +2 (1 increment + 1 nesting penalty)
                OtherCodeunit.Relay := Value;   // RecursionCycle: +1 (1 increment + 0 nesting penalty, no nesting penalty on recursion)

        while true do                           // WhileStatement: +1 (1 increment + 0 nesting penalty)
            OtherCodeunit.Relay := Value;       // RecursionCycle: +1 (1 increment + 0 nesting penalty, no nesting penalty on recursion)

        if true then                            // IfStatement: +1 (1 increment + 0 nesting penalty)
            if Condition then                   // IfStatement: +2 (1 increment + 1 nesting penalty)
                OtherCodeunit.Relay := Value    // RecursionCycle: +1 (1 increment + 0 nesting penalty, no nesting penalty on recursion)
            else                                // ElseStatement: +2 (1 increment + 1 nesting penalty)
                OtherCodeunit.Relay := Value;   // RecursionCycle: +1 (1 increment + 0 nesting penalty, no nesting penalty on recursion)

        repeat                                  // RepeatStatement: +1 (1 increment + 0 nesting penalty)
            OtherCodeunit.Relay := Value;       // RecursionCycle: +1 (1 increment + 0 nesting penalty, no nesting penalty on recursion)
        until true;
    end;
}

codeunit 50101 OtherCodeunit
{
    procedure Relay(Value: Integer)
    var
        MyCodeunit: Codeunit MyCodeunit;
    begin
        MyCodeunit.CountDown := Value - 1;
    end;
}
