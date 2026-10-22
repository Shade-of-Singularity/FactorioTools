using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactorioTools;

public static class JsonSerializerHelpers
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Deserialize<T>(string json, JsonSerializerContext context)
	{
		var result = JsonSerializer.Deserialize(json, typeof(T), context);
		if (result is T item)
			return item;

		Throw<T>(result);
		return default;
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	static void Throw<T>(object? input)
	{
		if (input is null)
			throw new SerializationException($"({typeof(T).Name}) deserialized to 'null'!");
		else
			throw new SerializationException($"Deserialized to a wrong type instead of {typeof(T).Name}!");
	}

}

public static class SerializerHelpers
{
	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static T ThrowNull<T>() => throw new SerializationException($"{typeof(T).Name} deserialized to null!");

}
