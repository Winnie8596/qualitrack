namespace QualiTrack.Domain.Tests;

public sealed class AuditStampTests
{
    [Fact]
    public void Audit_stamp_time_must_be_utc()
    {
        var kualaLumpurTime = new DateTimeOffset(2026, 10, 1, 17, 0, 0, TimeSpan.FromHours(8));

        var exception = Record.Exception(() => new AuditStamp(Ncrs.Inspector, kualaLumpurTime));

        Assert.Equal(DomainErrorKind.InvalidInput, Assert.IsType<DomainException>(exception).Kind);
    }
}
