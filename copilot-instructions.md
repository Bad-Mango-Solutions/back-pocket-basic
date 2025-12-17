# Repository Copilot Instructions

1. **Do not suppress warnings.** Disabling a warning is not the same as fixing it. Resolve the underlying issue or provide the required documentation instead of turning analyzers off.
2. **XML documentation completeness.** Write well-formed XML docs that include summaries plus documentation of parameters, type parameters, and return values when applicable.
3. **Use inheritdoc when appropriate.** If a class or member implements an already documented interface or inherited member, prefer `<inheritdoc cref="FullyQualifiedMember" />` to avoid duplication while keeping documentation intact.
4. **StyleCop compliance.** Follow repository code-style rules (including newline expectations) to keep StyleCop analyzers clean without suppressions.
