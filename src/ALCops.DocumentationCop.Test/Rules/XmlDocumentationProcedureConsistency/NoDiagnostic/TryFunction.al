codeunit 50100 MyCodeunit
{
    /// <summary>
    /// A method with TryFunction attribute has a implicit (boolean) return value.
    /// </summary>
    /// <returns>Returns success (true/false)</returns>
    [[|TryFunction|]]
    procedure MyTryFunction()
    begin

    end;
}