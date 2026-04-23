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

Browse the complete rules reference at [alcops.dev/docs/analyzers](https://alcops.dev/docs/analyzers/).

## Contributing

Contributions are welcome! Whether it's a new rule idea, a bug report, or a pull request — all input helps improve ALCops for the community.

- 💡 **Suggest a rule** » Open a [GitHub Discussion](https://github.com/ALCops/Analyzers/discussions)
- 🐛 **Report a bug** » File an [Issue](https://github.com/ALCops/Analyzers/issues/new)
- 🔧 **Submit a PR** » Fork the repo, create a branch, and open a pull request

## Thank you

ALCops is a continuation of [BusinessCentral.LinterCop](https://github.com/StefanMaron/BusinessCentral.LinterCop) and this project wouldn't exist without the foundation built by that community. A heartfelt thank you to every contributor who invested their time, ideas, and code into the original LinterCop. Your work didn't end there, it lives on and grows further here in ALCops.

## License

This project is licensed under the [MIT License](LICENSE).