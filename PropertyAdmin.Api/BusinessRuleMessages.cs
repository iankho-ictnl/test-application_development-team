namespace PropertyAdmin.Api;

/// <summary>
/// Resource anchor for the business-rule messages (D-049): the Logic project throws a
/// <see cref="PropertyAdmin.Logic.BusinessRuleException"/> with an English key, and the central error
/// handler returns it in the request language from Resources/BusinessRuleMessages.nl.resx. A message
/// without an entry (features not yet moved, still Dutch) is returned unchanged.
/// </summary>
public sealed class BusinessRuleMessages;
