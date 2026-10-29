namespace QualiTrack.Domain;

/// <summary>Thrown when a command would break a business rule. The entity is left unchanged.</summary>
public sealed class DomainException(string message) : Exception(message);
