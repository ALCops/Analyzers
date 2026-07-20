// Collision guard: two subscribers to the same event in one codeunit are a legal pattern.
// Both currently carry different wrong names, but both would compute to the same preferred
// name ("MyPublisher_OnSomething"). Renaming both at once would produce a duplicate-
// identifier compile error, so the analyzer stays silent on both. The developer must
// disambiguate (e.g. rename one manually with a suffix) before LC0098 offers a fix.
codeunit 50100 MySubscriber
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyPublisher, OnSomething, '', false, false)]
    local procedure [|WronglyNamedSubscriberA|]()
    begin
    end;

    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyPublisher, OnSomething, '', false, false)]
    local procedure [|WronglyNamedSubscriberB|]()
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
