codeunit 50100 ParameterPragmaCases
{
    procedure TargetPragmaPair(
        MyInteger: Integer;
        #pragma warning disable AA0010
        [|MyText: Text|];
        #pragma warning restore AA0010
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure PreviousPragmaPair(
        #pragma warning disable AA0005
        MyInteger: Integer;
        #pragma warning restore AA0005
        [|MyText: Text|];
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure NextPragmaPair(
        MyInteger: Integer;
        [|MyText: Text|];
        #pragma warning disable AA0005
        MyDate: Date
        #pragma warning restore AA0005
        )
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure MixedPragmaPair(
        MyInteger: Integer;
        #pragma warning disable AA0011
        [|MyText: Text|];
        MyDate: Date
        #pragma warning restore AA0011
        )
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure MixedPragmaPairComments(
        MyInteger: Integer;
        // before pragma pair
        /* also before pragma pair */
        #pragma warning disable AA0012
        // before parameter being removed
        [|MyText: Text|];
        // after parameter being removed
        // before retained parameter
        MyDate: Date
        #pragma warning restore AA0012
        // after pragma pair
        )
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure CrossProcedurePragma(
        MyInteger: Integer;
        #pragma warning disable AA0006
        [|MyText: Text|];
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure UnbalancedPragmaIds(
        MyInteger: Integer;
        #pragma warning disable AA0005
        [|MyText: Text|];
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure LastParameterPragma(
        MyInteger: Integer;
        #pragma warning disable AA0005
        [|MyText: Text|])
    begin
        MyInteger := 1;
    end;

    #pragma warning restore AA0006
    #pragma warning restore AA0007
}