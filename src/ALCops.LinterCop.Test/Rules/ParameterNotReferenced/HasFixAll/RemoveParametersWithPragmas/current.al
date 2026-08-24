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

    procedure AdjacentPragmaPairs(
        MyInteger: Integer;
        #pragma warning disable AA0013
        [|MyText: Text|];
        #pragma warning restore AA0013
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
        #pragma warning disable AA0015, AA0016
        [|MyText: Text|];
        #pragma warning restore AA0016,AA0015
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure DuplicatePragmaCodes(
        MyInteger: Integer;
        #pragma warning disable AA0025, AA0025
        [|MyText: Text|];
        #pragma warning restore AA0025
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure NestedPragmaPairs(
        #pragma warning disable AA0017
        MyInteger: Integer;
        #pragma warning disable AA0017
        [|MyText: Text|];
        #pragma warning restore AA0017
        [|MyCode: Code[20]|];
        #pragma warning restore AA0017
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure NestedDifferentPragmas(
        #pragma warning disable AA0026
        MyInteger: Integer;
        #pragma warning disable AA0027
        [|MyText: Text|];
        #pragma warning restore AA0027
        #pragma warning restore AA0026
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure MultiplePragmaPairs(
        MyInteger: Integer;
        #pragma warning disable AA0028
        #pragma warning disable AA0029
        [|MyText: Text|];
        #pragma warning restore AA0029
        #pragma warning restore AA0028
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
        [|ActiveParameter: Date|];
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
        [|ActiveParameter: Date|];
#endif
        MyInteger: Integer)
    begin
        #pragma warning restore AA0043
        MyInteger := 1;
    end;

    procedure LastBalancedPragma(
        MyInteger: Integer;
        #pragma warning disable AA0030
        [|MyText: Text|]
        #pragma warning restore AA0030
        )
    begin
        MyInteger := 1;
    end;

    procedure MethodBodyPragmaScope(
        #pragma warning disable AA0018
        [|MyText: Text|])
    begin
        #pragma warning restore AA0018
    end;

    procedure AllParametersPragmaScope(
        [|MyInteger: Integer|];
        #pragma warning disable AA0019
        [|MyText: Text|])
    begin
        #pragma warning restore AA0019
    end;

    procedure AllParametersNestedPragmas(
        #pragma warning disable AA0031
        [|MyInteger: Integer|];
        #pragma warning disable AA0032
        [|MyText: Text|])
    begin
        #pragma warning restore AA0032
        #pragma warning restore AA0031
    end;

    procedure SpecificDisableRestoreAll(
        MyInteger: Integer;
        #pragma warning disable AA0020
        [|MyText: Text|];
        #pragma warning restore
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure AllDisableSpecificRestore(
        MyInteger: Integer;
        #pragma warning disable
        [|MyText: Text|];
        #pragma warning restore AA0021
        MyDate: Date)
    begin
        MyInteger := 1;
        MyDate := Today();
    end;

    procedure PartialPragmaRestore(
        MyInteger: Integer;
        #pragma warning disable AA0022, AA0023
        [|MyText: Text|];
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