codeunit 50100 MyCodeunit
{
    /// <summary>
    /// Param tag without name attribute on procedure with parameters.
    /// </summary>
    /// [|<param></param>|]
    procedure WithParameter([|Value: Boolean|])
    begin

    end;

    /// <summary>
    /// Param tag without name attribute on procedure without parameters.
    /// </summary>
    /// [|<param></param>|]
    procedure WithoutParameter()
    begin

    end;

    /// <summary>
    /// Param tag without name attribute on procedure with parameters.
    /// </summary>
    /// [|<param>|]
    procedure WithParameterAlsoInvalid([|Value: Boolean|])
    begin

    end;
}
