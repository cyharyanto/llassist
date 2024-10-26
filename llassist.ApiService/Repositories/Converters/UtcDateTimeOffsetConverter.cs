using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace llassist.ApiService.Repositories.Converters;

public class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTime>
{
    // Always stores in UTC
    public UtcDateTimeOffsetConverter()
        : base(
            dto => dto.UtcDateTime,
            dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)))
    {
    }
}
