using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ViciOne.Serialization.Json;

public class JsonSerialization
{
    [SuppressMessage("Security", "CA2326:Do not use TypeNameHandling values other than None",
            Justification = "For correct functionality the serializer also has to handle user-defined types.")]
    [SuppressMessage("Security", "CA2328:Ensure that JsonSerializerSettings are secure",
            Justification = "For correct functionality the serializer also has to handle user-defined types.")]
    private static readonly JsonSerializerSettings s_defaultSettings = new()
    {
        ContractResolver = new WritablePropertiesOnlyContractResolver(),
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        PreserveReferencesHandling = PreserveReferencesHandling.All,
        SerializationBinder = CrossPlatformSerializationBinder.Instance,
        TypeNameHandling = TypeNameHandling.Auto,
    };

    public static JsonSerialization Default { get; } = new JsonSerialization();
    public JsonSerializerSettings Settings { get; set; } = s_defaultSettings;

    public T Load<T>(string value)
        => JsonConvert.DeserializeObject<T>(value, Settings)!;

    public T LoadFromFile<T>(string path)
        => Load<T>(File.ReadAllText(path));

    public string Save(object instance)
        => JsonConvert.SerializeObject(instance, Settings);

    public void Save(object instance, string path)
        => File.WriteAllText(path, Save(instance));
}
