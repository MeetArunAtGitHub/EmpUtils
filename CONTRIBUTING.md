# Contributing to EmpUtils

Thank you for contributing to EmpUtils. This file describes the conventions and processes contributors should follow.

## Getting started
- Fork the repository and create a feature branch named using the pattern `feature/<short-description>` or `bugfix/<short-description>`.
- Run the project in Visual Studio 2022. Use the __Git Changes__ or __Team Explorer__ windows for source control if you prefer the IDE.

## Coding standards
- Follow the rules defined in the repository's `.editorconfig` (C# 7.3, .NET Framework 4.8 Windows Forms project).
- Use 4 spaces for indentation and CRLF line endings.
- Keep lines under 120 characters where practical.

## Commits
- Use imperative, present-tense commit messages. Example: `Add resource filter to Resource form`.
- Create a single logical change per commit.

## Pull requests
- Open a pull request against `main` (or the repository default branch).
- Describe the change, include screenshots if UI changes occur, and list any migration steps or configuration changes.
- Ensure the PR passes automated checks (build and tests) before requesting review.

## Tests and validation
- Run any existing unit or integration tests locally before submitting a PR.
- Validate configuration values in `App.config` are not hard-coded secrets; use environment variables or secrets store for credentials.

## Style and review
- Keep changes small and focused.
- Reviewers should request changes for formatting, correctness, security, or missing error handling.

## Files to include
- Ensure you include any new source files, designer files, resource files, and updated project/solution files.

## License and code of conduct
- By contributing you agree your contributions will be licensed under the repository license.