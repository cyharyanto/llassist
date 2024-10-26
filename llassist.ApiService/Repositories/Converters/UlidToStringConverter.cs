using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace llassist.ApiService.Repositories.Converters;

public class UlidToStringConverter : ValueConverter<Ulid, string>
{
    public UlidToStringConverter()
        : base(
            ulid => ulid.ToString(),
            str => Ulid.Parse(str))
    {
    }
}
