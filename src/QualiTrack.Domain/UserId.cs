namespace QualiTrack.Domain;

public readonly record struct UserId
{
    public UserId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        Value = value;
    }

    public Guid Value { get; }
}
