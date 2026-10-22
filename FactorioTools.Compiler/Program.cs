using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// Workflow:
/// 1. Locate all mod.json files.
///   - Fast fail if none are found.
/// 2. Locate deploy.toml in the same folder. Exclude it during compilation.
///   - Create new one if none are found, and quit.
/// 3. Create temp folder for each mod. Clear if already exists.
/// 4. Locate all special Factorio files.
/// 5. Locate any control files. Request loading of all files in all special folders via them.
///   - Append require at the end (if allowed by syntax), so root control files can serve as an init point.
/// 6. 
/// Features:
/// 1. Single-threaded - wanna go casual for now.
Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
$"[ = ]   Welcome to Factorio Tools v{Assembly.GetExecutingAssembly().GetName().Version}!   [ = ]".Out(ConsoleColor.Cyan);
$"".Out();
$"Locating mods...".Out(ConsoleColor.Gray);

var root = AppContext.BaseDirectory;
var subs = Directory.GetDirectories(root);
List<ModInfo> mods = [];
foreach (var sub in subs)
{
	var files = Directory.GetFiles(sub, "mod.json");
	if (files.Length == 0) continue;
	var file = files[0];

	try
	{
		string content = File.ReadAllText(file);
		var info = JsonSerializer.Deserialize(content, SerializerOptions.JsonOptions);
		mods.Add(info);
	}
	catch (Exception ex)
	{
		$"Failed to deserialize 'mod.json' at: {file}\n{ex.ToDisplay()}".Out(ConsoleColor.Red);
	}
}

$"Located mods:".Out();
foreach (var mod in mods)
{

}
readonly record struct ModMeta();

sealed partial class ModInfo
{
	[JsonSerializable(typeof(ModInfo))]
	[JsonSourceGenerationOptions(WriteIndented = true)]
	public sealed partial class JsonContext : JsonSerializerContext;

	[JsonPropertyName("name")] public string? Name { get; set; }
	[JsonPropertyName("version")] public string? Version { get; set; }

	public override string ToString() => $"{Name} v{Version}";

	// Unused properties:
	//[JsonPropertyName("title")] public string? Title { get; set; }
	//[JsonPropertyName("author")] public string? Author { get; set; }
	//[JsonPropertyName("contact")] public string? Contact { get; set; }
	//[JsonPropertyName("homepage")] public string? Homepage { get; set; }
	//[JsonPropertyName("factorio_version")] public string? FactorioVersion { get; set; }
	//[JsonPropertyName("description")] public string? Description { get; set; }
	//[JsonPropertyName("dependencies")] public List<string>? Dependencies { get; set; }
	//[JsonPropertyName("quality_required")] public bool? QualityRequired { get; set; }
	//[JsonPropertyName("rail_bridges_required")] public bool? RailBridgesRequired { get; set; }
	//[JsonPropertyName("spoiling_required")] public bool? SpoilingRequired { get; set; }
	//[JsonPropertyName("freezing_required")] public bool? FreezingRequired { get; set; }
	//[JsonPropertyName("segmented_units_required")] public bool? SegmentedUnitsRequired { get; set; }
	//[JsonPropertyName("expansion_shaders_required")] public bool? ExpansionShadersRequired { get; set; }
	//[JsonPropertyName("expansion_required")] public bool? ExpansionRequired { get; set; }
	//[JsonPropertyName("space_travel_required")] public bool? SpaceTravelRequired { get; set; }
}