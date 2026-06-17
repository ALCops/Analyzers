codeunit 50100 MyCodeunit
{
    /// <summary>
    /// Triggered just before the world goes light.
    /// </summary>
	/// <param name="i">An integer value.</param>
	/// <param name="d">A decimal value.</param>
	/// <param name="returnText">A text to return.</param>
    [IntegrationEvent(false, false)]
    local procedure [|OnBeforeTheWorldGoesLight|](i: Integer; d: Decimal; var returnText: Text)
    begin
    end;
}