using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QualiTrack.Domain;

namespace QualiTrack.Infrastructure.Persistence.Configurations;

internal sealed class UserIdConverter() : ValueConverter<UserId, Guid>(user => user.Value, value => new UserId(value));
