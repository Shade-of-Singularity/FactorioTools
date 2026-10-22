using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Serialization;

/// Workflow:
/// 1. Locate all mod.json files.
///   - Fast fail if none are found.
/// 2. Locate deploy.toml in the same folder. Exclude it during compilation.
///   - Create new one if none are found, and quit.
/// 3. Create temp folder for each mod. Clear if already exists.
/// 4. Locate all special Factorio files.
/// 5. Locate any control files. Request loading of all files in all special folders via them.
///   - Append require at the end (if allowed by syntax), so root control files can serve as an init point.
/// 6. Remove unused FactorioTools and Mod files.
/// Features:
/// 1. Single-threaded - wanna go casual for now.
Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;
try
{
	$"[ = ]   Welcome to Factorio Tools v{Assembly.GetExecutingAssembly().GetName().Version}!   [ = ]".Out(ConsoleColor.Cyan);
	string.Join(", ", args).Out(ConsoleColor.Yellow);

	if (args.Length == 0)
	{
		$"No specific folders were specified.".Out();
		$"We will try to locate mods in current directory instead.".Out();
		StageAll(AppContext.BaseDirectory);
	}
	else
	{
		$"Provided total of ({args.Length}) locations to scout.".Out();
		$"All mods at provided locations will be compiled.".Out();
		foreach (var arg in args)
			StageAll(root: arg);
	}
}
catch (Exception ex)
{
	// Global exception handling.
	ex.Out("Unhandled exception:\n");
}

FinishSession();


/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===<![CDATA[
/// .
/// .                                               Main Workflow
/// .
/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===]]>
static void StageAll(string root)
{
	$"".Out();
	$"Processing location: {root}".Out();
	if (!Path.Exists(root))
	{
		$"Location cannot be found: {root}".Out(ConsoleColor.Red);
		return;
	}




	/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===<![CDATA[
	/// .
	/// .                                                  Scanning
	/// .
	/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===]]>
	List<ModMeta> mods = [];

	// Scanning root folder.
	const string ModInfoFileName = "info.json";
	const string DeployFileName = "deploy.toml";
	string file = Path.Combine(root, ModInfoFileName);
	if (File.Exists(file))
		ProcessLocation(mods, root, file);

	// And then its sub folders, just in case.
	// Note: we might provide recursion depth if needed later.
	var subs = Directory.GetDirectories(root);
	foreach (var sub in subs)
	{
		file = Path.Combine(sub, ModInfoFileName);
		if (File.Exists(file))
			ProcessLocation(mods, sub, file);
	}

	// Dedicated function for processing current file location.
	static void ProcessLocation(List<ModMeta> mods, string modLocation, string file)
	{
		ModInfo info;
		try
		{
			string content = File.ReadAllText(file);
			info = JsonSerializerHelpers.Deserialize<ModInfo>(content, ModInfo.JsonContext.Default);
		}
		catch (Exception ex)
		{
			ex.Out($"Failed to deserialize '{ModInfoFileName}' at: {file}\n");
			return;
		}

		file = Path.Combine(modLocation, DeployFileName);
		if (!File.Exists(file))
		{
			$"Mod ({info.Name}) is missing '{DeployFileName}' file. New one will be created in the same folder.".Out(ConsoleColor.Yellow);
			try
			{
				string content = TomlSerializer.Serialize(DeployInfo.Default, TomlContext.Default.DeployInfo);
				File.WriteAllText(file, content);
			}
			catch (Exception ex)
			{
				ex.Out($"Default '{DeployFileName}' creation failed!\n");
			}
			return;
		}

		DeployInfo deploy;
		try
		{
			string content = File.ReadAllText(file);
			deploy = TomlSerializer.Deserialize(content, TomlContext.Default.DeployInfo)
				?? SerializerHelpers.ThrowNull<DeployInfo>();
		}
		catch (Exception ex)
		{
			ex.Out($"Failed to deserialize '{DeployFileName}' at: {file}!\n");
			return;
		}

		mods.Add(new(modLocation, info, deploy));
	}




	/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===<![CDATA[
	/// .
	/// .                                                 Mod Listing
	/// .
	/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===]]>
	$"Located mods:".Out();
	mods.ForEach(static m => m.Info.Out("- "));
	if (mods.Count == 0)
	{
		$"No mods founds!".Out(ConsoleColor.Yellow);
		return;
	}




	/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===<![CDATA[
	/// .
	/// .                                                Compilation
	/// .
	/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===]]>
	foreach (var mod in mods)
	{
		$"".Out();
		$"Compiling mod ({mod.Info})...".Out();
		string output;
		try
		{
			output = PathHelpers.ReplaceSpecialFolders(mod.Deploy.Output);
		}
		catch (Exception ex)
		{
			ex.Out($"Cannot format output location! Check if deploy.toml is properly formatted.\n");
			continue;
		}

		// Copying to temp.
		string temp = Path.Combine(root, "temp");
		try { Directory.Delete(temp, true); }
		catch (DirectoryNotFoundException) { /* Handling exception is safer in concurrent contexts. */}
		catch (Exception ex)
		{
			ex.Out($"Failed to delete an existing 'temp' directory! Compilation will stop.\n");
			continue;
		}

		// Creating special folder with a name, matching mod.json.
		// This is required by the Factorio itself.
		string zip = Path.Combine(temp, $"{mod.Info.Name ?? string.Empty}");
		try
		{
			DirectoryHelpers.Copy(mod.Location, zip, true);
		}
		catch (Exception ex)
		{
			ex.Out($"Failed to copy mod files to a temp folder!\n");
			continue;
		}

		// Staging:
		Stage(zip, "data");
		Stage(zip, "data-updates");
		Stage(zip, "data-final-fixes");
		Stage(zip, "settings");
		Stage(zip, "settings-updates");
		Stage(zip, "settings-final-fixes");
		Stage(zip, "control");
		Stage(zip, "space-data");
		Stage(zip, "space-data-updates");
		Stage(zip, "space-data-final-fixes");

		// Removing unnecessary files.
		DeleteFile(Path.Combine(zip, DeployFileName));
		DeleteFiles(zip, "*.pdb");
		DeleteFiles(zip, "*.exe");

		// Archiving:
		FileStream? stream = null;
		try
		{
			string location = Path.Combine(output, $"{mod.Info.Name}_{mod.Info.Version}.zip");
			stream = File.Open(location, FileMode.Truncate, FileAccess.Write, FileShare.None);
			ZipFile.CreateFromDirectory(temp, stream);
		}
		catch (Exception ex)
		{
			try { Directory.Delete(temp, true); }
			catch { /* Ignored, as to not confuse the user. */ }
			ex.Out();
			continue;
		}
		finally { stream?.Dispose(); }

		try { Directory.Delete(temp, true); }
		catch (DirectoryNotFoundException) { /* Ignored. Might happen under concurrency. */ }
		catch (Exception ex) { ex.Out("Failed to delete temp folder!\n", ConsoleColor.Yellow); }
		$"Mod ({mod.Info}) compiled successfully!".Out(ConsoleColor.Cyan);
	}
}




/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===<![CDATA[
/// .
/// .                                              Helper Functions
/// .
/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===]]>
static void FinishSession()
{
	Console.WriteLine();
	Console.Write($"Press SUBMIT to close the window...");
	Console.ReadLine();
}

/// root - is a root folder where staging happens.
/// control - is a sub-folder within a root. data.lua will point to files in root/data.
static void Stage(string root, string control)
{
	//$"Staging (at: {root}) (sub: {control})".Out(ConsoleColor.DarkGray);
	string path = Path.Combine(root, control);
	if (Directory.Exists(path))
	{
		FileStream? stream = null;
		StreamWriter? writer = null;
		try
		{
			bool hasPostfix = false;
			foreach (var file in Directory.EnumerateFiles(path, "*.lua"))
			{
				FileInfo info = new(file);
				if (info.Length == 0)
					info.Delete();
				else
				{
					// Only open the root file if there are any non-empty files.
					if (stream is null || writer is null)
					{
						stream = File.Open(path + ".lua", FileMode.Append, FileAccess.Write);
						writer = new(stream, Encoding.UTF8);
					}

					if (!hasPostfix)
					{
						writer.WriteLine();
						writer.WriteLine();
						hasPostfix = true;
					}

					// Trims path to only: data/location
					string link = file[root.Length..];

					// Removes extension if exists.
					ReadOnlySpan<char> span = link.AsSpan();
					var extension = Path.GetExtension(span);
					if (!extension.IsEmpty)
						link = link[..^extension.Length];

					// Replaces path symbols with alternative, required by Factorio LUA.
					link = link.Replace('\\', '/');

					// Writes a required statement.
					writer.WriteLine($"require(\"{link}\")");
				}
			}

			writer?.Flush();
		}
		finally
		{
			writer?.Dispose();
			stream?.Dispose();
		}

		if (!Directory.EnumerateFiles(path).Any())
		{
			try { Directory.Delete(path, true); }
			catch (Exception ex) { ex.Out($"Failed to delete an empty subdirectory: {control}"); }
		}
	}
}

static void DeleteFile(string path, bool log = true)
{
	try { File.Delete(path); }
	catch (Exception ex) { if (log) ex.Out($"Failed to delete unnecessary file: {path}\n"); }
}
static void DeleteFiles(string path, string searchPattern, bool log = true)
{
	foreach (var f in Directory.EnumerateFiles(path, searchPattern))
	{
		try { File.Delete(f); }
		catch (Exception ex) { if (log) ex.Out($"Failed to delete unnecessary file: {f}\n"); }
	}
}



/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===<![CDATA[
/// .
/// .                                             Structs and Classes
/// .
/// ===     ===     ===     ===    ===  == =  -                        -  = ==  ===    ===     ===     ===     ===]]>
readonly record struct ModMeta(string Location, ModInfo Info, DeployInfo Deploy);
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

sealed partial class DeployInfo
{
	public static readonly DeployInfo Default = new()
	{
		Output = "%APPDATA%/Factorio/mods",
	};

	[TomlPropertyName("output")] public required string Output { get; set; }
}

[TomlSerializable(typeof(DeployInfo))]
[TomlSourceGenerationOptions(WriteIndented = true)]
sealed partial class TomlContext : TomlSerializerContext;