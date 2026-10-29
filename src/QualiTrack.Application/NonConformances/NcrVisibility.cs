using QualiTrack.Application.Abstractions;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.NonConformances;

/// <summary>
/// Which NCRs each role may see (PRD §4, D1): an inspector those they raised, an investigator those raised by
/// or assigned to them, a QA manager all. Here, not in the domain, because roles are an application concept.
/// </summary>
public static class NcrVisibility
{
    public static bool CanView(this ICurrentUser user, NonConformance ncr) => user.Role switch
    {
        Role.Inspector => ncr.Raised.By == user.Id,
        Role.Investigator => ncr.Raised.By == user.Id || ncr.InvestigatorId == user.Id,
        Role.QaManager => true,
        _ => false,
    };
}
