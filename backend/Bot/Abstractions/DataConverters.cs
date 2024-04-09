using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;

namespace Bot.Abstractions;

public class JsonDataConverter<T> : ValueConverter<T, string>
{
    public JsonDataConverter() : base(
        v => JsonConvert.SerializeObject(v),
        v => JsonConvert.DeserializeObject<T>(v)
    )
    {
    }
}

public class ListDataConverter<TKey> : JsonDataConverter<List<TKey>>
{
}

public class DictionaryDataConverter<TKey, TValue> : JsonDataConverter<Dictionary<TKey, TValue>>
{
}
