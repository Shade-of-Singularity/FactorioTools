using System.Text;

namespace FactorioTools;

public static class PathHelpers
{
	static readonly StringBuilder Builder = new();
	public static string ReplaceSpecialFolders(string path) =>
		FastReplace(path, "%APPDATA%", Environment.SpecialFolder.ApplicationData);

	static string FastReplace(string str, string key, Environment.SpecialFolder folder)
	{
		int index = str.IndexOf(key);
		if (index == -1) return str;
		Builder.Clear();
		Builder.Append(str, 0, index);
		Builder.Append(Environment.GetFolderPath(folder));
		Builder.Append(str, index + key.Length, str.Length - index - key.Length);
		return Builder.ToString();
	}
}
