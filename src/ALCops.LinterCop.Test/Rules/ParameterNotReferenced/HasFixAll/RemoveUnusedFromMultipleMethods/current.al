codeunit 50100 FixAllMultipleMethods
{
    procedure FirstProcedure(
        MyInteger: Integer;
        #pragma warning disable AA0040
        [|UnusedA: Text|]
        #pragma warning restore AA0040
        )
    begin
        MyInteger := 1;
    end;

    procedure SecondProcedure(
        #pragma warning disable AA0041
        [|UnusedB: Date|];
        #pragma warning restore AA0041
        MyText: Text)
    begin
        MyText := 'x';
    end;

    procedure ThirdProcedure(MyInteger: Integer; [|UnusedA: Text|])
    begin
        MyInteger := 1;
    end;

    procedure FourthProcedure([|UnusedB: Date|]; MyText: Text)
    begin
        MyText := 'x';
    end;
}
