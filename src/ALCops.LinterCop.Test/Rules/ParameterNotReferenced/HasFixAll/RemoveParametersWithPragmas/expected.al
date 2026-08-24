codeunit 50100 ParameterPragmaCases
{
    procedure TargetPragmaPair(
        MyInteger: Integer;
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure AdjacentPragmaPairs(
        MyInteger: Integer;
        #pragma warning disable AA0014
        MyDate: Date
        #pragma warning restore AA0014
        )
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure MultiCodePragmaPair(
        MyInteger: Integer;
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure DuplicatePragmaCodes(
        MyInteger: Integer;
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure NestedPragmaPairs(
        #pragma warning disable AA0017
        MyInteger: Integer;
        #pragma warning restore AA0017
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure NestedDifferentPragmas(
        #pragma warning disable AA0026
        MyInteger: Integer;
        #pragma warning restore AA0026
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure MultiplePragmaPairs(
        MyInteger: Integer;
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure ConditionalPragmaBranches(
#if ACTIVE
        #pragma warning disable AA0037
        InactiveParameter: Text;
        #pragma warning restore AA0037
#else
        #if INNER
        #pragma warning disable AA0038
        NestedInactiveParameter: Code[20];
        #pragma warning restore AA0038
        #else
        #pragma warning disable AA0039
        ActiveParameter: Date;
        #pragma warning restore AA0039
        #endif
#endif
        MyInteger: Integer)
    begin
        MyInteger := 1;
    end;

    procedure ConditionalMethodBodyPragma(
#if ACTIVE
        InactiveParameter: Text;
#else
        #pragma warning disable AA0043
        ActiveParameter: Date;
#endif
        MyInteger: Integer)
    begin
        #pragma warning restore AA0043
        MyInteger := 1;
    end;

    procedure LastBalancedPragma(
        MyInteger: Integer)
    begin
        MyInteger := 1;
    end;

    procedure MethodBodyPragmaScope(
        #pragma warning disable AA0018
        )
    begin
        #pragma warning restore AA0018
    end;

    procedure AllParametersPragmaScope(
        #pragma warning disable AA0019
        )
    begin
        #pragma warning restore AA0019
    end;

    procedure AllParametersNestedPragmas(
        #pragma warning disable AA0031
        #pragma warning disable AA0032
        )
    begin
        #pragma warning restore AA0032
        #pragma warning restore AA0031
    end;

    procedure SpecificDisableRestoreAll(
        MyInteger: Integer;
        #pragma warning disable AA0020
        #pragma warning restore
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure AllDisableSpecificRestore(
        MyInteger: Integer;
        #pragma warning disable
        #pragma warning restore AA0021
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure PartialPragmaRestore(
        MyInteger: Integer;
        #pragma warning disable AA0022, AA0023
        #pragma warning restore AA0022
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