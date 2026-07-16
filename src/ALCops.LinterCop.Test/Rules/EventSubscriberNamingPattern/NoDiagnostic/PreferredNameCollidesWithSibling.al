// Collision guard: the subscriber below currently carries a wrong name, but a sibling
// procedure "MyPublisher_OnSomething" already exists in the same codeunit — this is the
// canonical name the CodeFix would suggest. Applying the fix would duplicate the identifier,
// so the analyzer stays silent and lets the developer resolve the collision manually.
codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyPublisher, OnSomething, '', false, false)]
    local procedure [|WronglyNamedSubscriber|]()
    begin
    end;

    // Pre-existing plain procedure with the exact name LC0098 would suggest for the subscriber above.
    // The name collision must suppress the diagnostic.
    local procedure "MyPublisher_OnSomething"()
    begin
    end;
}

codeunit 50101 MyPublisher
{
    [IntegrationEvent(false, false)]
    procedure OnSomething()
    begin
    end;
}
