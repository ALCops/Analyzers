codeunit 50100 MyCodeunit
{
    procedure [|MyProcedure|]()         // Cognitive Complexity: 15 (threshold >=15)
    var
        Condition: Boolean;
    begin
        if true then                    // IfStatement: +1 (1 increment + 0 nesting penalty)
            if true then                // IfStatement: +2 (1 increment + 1 nesting penalty)
                SomeOtherProcedure();   // RecursionCycle: +1 (1 increment + 0 nesting penalty, no nesting penalty on recursion)

        while true do                   // WhileStatement: +1 (1 increment + 0 nesting penalty)
            SomeOtherProcedure();       // RecursionCycle: +1 (1 increment + 0 nesting penalty, no nesting penalty on recursion)

        if true then                    // IfStatement: +1 (1 increment + 0 nesting penalty)
            if Condition then           // IfStatement: +2 (1 increment + 1 nesting penalty)
                SomeOtherProcedure()    // RecursionCycle: +1 (1 increment + 0 nesting penalty, no nesting penalty on recursion)
            else                        // ElseStatement: +2 (1 increment + 1 nesting penalty)
                SomeOtherProcedure();   // RecursionCycle: +1 (1 increment + 0 nesting penalty, no nesting penalty on recursion)

        repeat                          // RepeatStatement: +1 (1 increment + 0 nesting penalty)
            this.SomeOtherProcedure();  // RecursionCycle: +1 (1 increment + 0 nesting penalty, no nesting penalty on recursion)
        until true;
    end;

    procedure SomeOtherProcedure()
    begin
        AndAnotherOne();
    end;

    procedure AndAnotherOne()
    begin
        MyProcedure();
    end;
}