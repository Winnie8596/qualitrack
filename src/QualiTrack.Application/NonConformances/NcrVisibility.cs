using System.Linq.Expressions;
using QualiTrack.Application.Abstractions;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Application.NonConformances;

/// <summary>
/// Which NCRs each role may see (PRD §4, D1): an inspector those they raised, an investigator those raised by
/// or assigned to them, a QA manager all. Here, not in the domain, because roles are an application concept.
/// Written as an expression so the NCR list filters on it in SQL and the detail query checks one NCR in memory.
/// </summary>
public static class NcrVisibility
{
    public static Expression<Func<NonConformance, bool>> VisibleNcrs(this ICurrentUser user)
    {
        UserId id = user.Id;
        return user.Role switch
        {
            Role.Inspector => ncr => ncr.Raised.By == id,
            Role.Investigator => ncr => ncr.Raised.By == id || ncr.InvestigatorId == id,
            Role.QaManager => ncr => true,
            _ => ncr => false,
        };
    }

    // Compiling costs microseconds, which one NCR per request can afford; the list never compiles it
    public static bool CanView(this ICurrentUser user, NonConformance ncr) => user.VisibleNcrs().Compile()(ncr);
}
