using ScsSitecoreResourceManager.Data.Properties;
using ScsSitecoreResourceManager.Data.Properties.Collectors;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc.Html;

namespace ScsSitecoreResourceManager.Pipelines.SitecoreResourceManager
{
	public class ReplaceAllTokens
	{
		public void Process(SitecoreResourceManagerArgs args)
		{
			Stack<string> directories = new Stack<string>(args.NewOverlayDirectories.Reverse());
			List<string> resolvedDirectories = new List<string>();
			List<string> resolvedFiles = new List<string>();
			while (directories.Count > 0)
			{
				var directory = directories.Pop();
				var processedDirectory = ReplaceTokens(directory.Substring(0, directory.LastIndexOf('\\')), args, directory) + '\\' + Path.GetFileName(directory);
				resolvedDirectories.Add(ProcessName(processedDirectory, args));
				args.EventLog.Add($"Replacing all tokens in folder name {processedDirectory}");
			}
			Stack<string> files = new Stack<string>(args.NewOverlayFiles.Reverse());
			while(files.Count > 0)
			{
				var file = files.Pop();
				var processedFile = ReplaceTokens(file.Substring(0, file.LastIndexOf('\\')), args, file) + '\\' + Path.GetFileName(file);
				ProcessFile(processedFile, args);
				args.EventLog.Add($"Replacing all tokens in file {processedFile}");
				resolvedFiles.Add(ProcessName(processedFile, args));
				args.EventLog.Add($"Replacing all tokens in file name {processedFile}");
			}

			args.NewOverlayDirectories = resolvedDirectories;
			args.NewOverlayFiles = resolvedFiles;
			


		}
		private static string ProcessName(string path, SitecoreResourceManagerArgs args)
		{
			var resolved = ReplaceTokens(path, args, Path.GetFileName(path));
			if (resolved != path)
			{
				try
				{
					if (Directory.Exists(resolved))
					{
						foreach (var child in Directory.EnumerateFileSystemEntries(path))
						{
							if (Directory.Exists(child))
							{
								Directory.Move(child, resolved + $@"\{Path.GetFileName(child)}");
							}
							else
							{
								File.Move(child, resolved + $@"\{Path.GetFileName(child)}");
							}
						}
						Directory.Delete(path);
					}
					else
					{
						Directory.Move(path, resolved);
					}
				}
				catch (IOException e)
				{
					throw new IOException($"Problem renaming file {path}.  This is likely due to another project with the same name, remove the old project and try again.", e);
				}
			}
			return resolved;
		}
		private static void ProcessFile(string file, SitecoreResourceManagerArgs args)
		{
			var txt = File.ReadAllText(file);
			try
			{
				File.WriteAllText(file, ReplaceTokens(txt, args, Path.GetFileName(file)));
			}catch(IOException e)
			{
				throw new IOException($"Unable to write to {file}", e);
			}
		}

		public static string ReplaceTokens(string text, SitecoreResourceManagerArgs args, string name = null)
		{
			return ReplaceTokens(text, args.ContainsKey, x => args[x], args, name);
		}
		public static string ReplaceTokens(string text, Func<string, bool> containsKey, Func<string, string> getValue, SitecoreResourceManagerArgs args = null, string name = null)
		{
			StringBuilder sb = new StringBuilder(text);
			bool token = false;
			bool delimiter = false;
			List<char> tokenName = new List<char>();
			for (int i = text.Length - 1; i >= 0; i--)
			{
				if (text[i] == '_')
				{
					if (token)
					{
						tokenName.Add('_');
						tokenName.Reverse();
						string tmp = new string(tokenName.ToArray());
						if (containsKey(tmp))
						{
							sb.Remove(i, tmp.Length);
							sb.Insert(i, getValue(tmp));
							args?.EventLog.Add($"Replaced token {tmp} with {getValue(tmp)} at index {i} of {name}");
						}
						else
						{
							Log.Error($"Unable to locate property {tmp} validate that it is being collected", text);
						}
						tokenName.Clear();
					}
					token = !token;
					delimiter = false;
				}else if (token && text[i] == '|')
				{
					delimiter = !delimiter;
				}
				else if (!char.IsUpper(text[i]) && !delimiter)
				{
					token = false;
					tokenName.Clear();
				}
				if (token)
				{
					tokenName.Add(text[i]);
				}
			}
			return sb.ToString();
		}
	}
}
