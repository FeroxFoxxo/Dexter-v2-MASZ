using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bot.Abstractions;

public abstract class DataContext<TContext>(DbContextOptions<TContext> options) : DbContext(options)
    where TContext : DbContext
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<ulong[]>().HaveConversion<UlongArrayConverter>();
        configurationBuilder.Properties<string[]>().HaveConversion<StringArrayConverter>();
        configurationBuilder.Properties<float[]>().HaveConversion<FloatArrayConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(GetType().Namespace?.Split('.').FirstOrDefault());

        StringArrayComparer stringArrayComparer = new();

        UlongArrayComparer ulongArrayComparer = new();

        FloatArrayComparer floatArrayComparer = new();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(ulong[]))
                    property.SetValueComparer(ulongArrayComparer);
                else if (property.ClrType == typeof(string[]))
                    property.SetValueComparer(stringArrayComparer);
                else if (property.ClrType == typeof(float[]))
                    property.SetValueComparer(floatArrayComparer);
            }

        OverrideModelCreating(modelBuilder);
    }

    public virtual void OverrideModelCreating(ModelBuilder modelBuilder)
    {
    }
}

public class UlongArrayConverter : ValueConverter<ulong[], string>
{
    public UlongArrayConverter() :
        base(
            v => string.Join(',', v),
            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ulong.Parse).ToArray()
        )
    {
    }
}

public class UlongArrayComparer : ValueComparer<ulong[]>
{
    public UlongArrayComparer()
        : base(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => (ulong[])c.Clone()
        )
    {
    }
}

public class FloatArrayConverter : ValueConverter<float[], string>
{
    public FloatArrayConverter()
        : base(
            v => string.Join(',', v.Select(n => $"{n:G6}")),
            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(float.Parse).ToArray()
        )
    {
    }
}

public class FloatArrayComparer : ValueComparer<float[]>
{
    public FloatArrayComparer()
        : base(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => (float[])c.Clone()
        )
    {
    }
}

public class StringArrayConverter : ValueConverter<string[], string>
{
    public StringArrayConverter() :
        base(
            v => string.Join(',', v),
            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
        )
    {
    }
}

public class StringArrayComparer : ValueComparer<string[]>
{
    public StringArrayComparer()
        : base(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => (string[])c.Clone()
        )
    {
    }
}
