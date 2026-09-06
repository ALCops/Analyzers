# ALCops Analyzers

[![NuGet](https://img.shields.io/nuget/v/ALCops.Analyzers?logo=nuget&label=NuGet)](https://www.nuget.org/packages/ALCops.Analyzers)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ALCops.Analyzers?logo=nuget&label=Downloads)](https://www.nuget.org/packages/ALCops.Analyzers)
[![Build](https://img.shields.io/github/actions/workflow/status/ALCops/Analyzers/build-and-release.yml?logo=github&label=Build)](https://github.com/ALCops/Analyzers/actions)
[![License](https://img.shields.io/github/license/ALCops/Analyzers)](LICENSE)

A collection of custom code analyzers for the AL programming language of Microsoft Dynamics 365 Business Central. ALCops ships **multiple specialized cops** covering everything from platform correctness and application modeling to documentation, formatting, linting, and test structure.

**Full documentation:** [http://www.alcops.dev](https://www.alcops.dev).

## Analyzers

| Cop | Description |
|-----|-------------|
| [ApplicationCop](https://alcops.dev/docs/analyzers/applicationcop/) | Validates rules that enforce correct modeling and behavior of Business Central objects, ensuring domain-consistent tables, pages, permissions, and metadata. Focuses on application correctness rather than AL language semantics. |
| [DocumentationCop](https://alcops.dev/docs/analyzers/documentationcop/) | Enforces documentation quality in code, such as procedure comments and developer-facing descriptions. Ensures clarity of intent without affecting runtime behavior. |
| [FormattingCop](https://alcops.dev/docs/analyzers/formattingcop/) | Covers stylistic and syntactic consistency rules. Ensures clean, uniform, readable code without influencing behavior or semantics. |
| [LinterCop](https://alcops.dev/docs/analyzers/lintercop/) | Identifies non-breaking code smells and suggests better implementation patterns. Focuses on maintainability, clarity, and recommended practices where multiple valid options exist. |
| [PlatformCop](https://alcops.dev/docs/analyzers/platformcop/) | Validates AL language and runtime semantic correctness, preventing patterns that always fail or behave unpredictably. These rules apply universally, independent of the Business Central domain model. |
| [TestAutomationCop](https://alcops.dev/docs/analyzers/testautomationcop/) | Ensures correctness and structure of test codeunits and related test procedures. Applies exclusively to test logic, not production code. |
| [Common](https://alcops.dev/docs/analyzers/common/) | Cross-cutting diagnostics from the shared ALCops.Common library, loaded with every cop — for example a warning when your `alcops.json` cannot be loaded. |

Browse the complete rules reference at [alcops.dev/docs/analyzers](https://alcops.dev/docs/analyzers/).

## Configuration

Analyzer-specific settings are configured in `alcops.json`. A project can inherit a centrally maintained base configuration from one anonymously accessible HTTP(S) URL or absolute local file path and override only the values it needs:

```json
{
  "Extends": {
    "Source": "https://example.com/company.alcops.json"
  },
  "SubscriberNamingPattern": "{Event Source}_{Event Name}[_{Element Name}]"
}
```

Local scalar values and arrays replace inherited values. Nested objects are merged property by property. Inheritance chains are deliberately not supported. See the [configuration guide](https://alcops.dev/docs/getting-started/configuration/) for all settings and precedence rules.

HTTP(S) sources must be anonymously accessible. URLs containing embedded credentials such as `https://user:pass@example.com/alcops.json` are rejected before network access, and their username/password are omitted from diagnostics. Committing `alcops.json` means trusting its referenced configuration source.

HTTP responses are limited to 1 MiB (1,048,576 bytes), with a five-second timeout. If a declared `Extends` source cannot be resolved, ALCops uses the built-in defaults for the entire configuration, discards local overrides, and reports `CM0001`.

Failed HTTP requests are retried by a later compilation; successful loads remain cached for the analyzer session. The first analysis using an uncached HTTP source can wait for the request, and cancelling that analysis also cancels the request. Empty, comment-only or JSON-null local configuration uses defaults without a warning; an inherited configuration must contain a JSON object.

## Contributing

Contributions are welcome! Whether it's a new rule idea, a bug report, or a pull request — all input helps improve ALCops for the community.

- 💡 **Suggest a rule** » Open a [GitHub Discussion](https://github.com/ALCops/Analyzers/discussions)
- 🐛 **Report a bug** » File an [Issue](https://github.com/ALCops/Analyzers/issues/new)
- 🔧 **Submit a PR** » Fork the repo, create a branch, and open a pull request

## Thank you

ALCops is a continuation of [BusinessCentral.LinterCop](https://github.com/StefanMaron/BusinessCentral.LinterCop) and this project wouldn't exist without the foundation built by that community. A heartfelt thank you to every contributor who invested their time, ideas, and code into the original LinterCop. Your work didn't end there, it lives on and grows further here in ALCops.

## License

This project is licensed under the [MIT License](LICENSE).
