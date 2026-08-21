codeunit 50100 ParameterPragmaCases
{
    procedure TargetPragmaPair(
        MyInteger: Integer;
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure PreviousPragmaPair(
        #pragma warning disable AA0005
        MyInteger: Integer;
        #pragma warning restore AA0005
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure NextPragmaPair(
        MyInteger: Integer;
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
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure UnbalancedPragmaIds(
        MyInteger: Integer;
        #pragma warning disable AA0005
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure LastParameterPragma(
        MyInteger: Integer
        #pragma warning disable AA0005
        )
    begin
        MyInteger := 1;
    end;

    #pragma warning restore AA0006
    #pragma warning restore AA0007
}