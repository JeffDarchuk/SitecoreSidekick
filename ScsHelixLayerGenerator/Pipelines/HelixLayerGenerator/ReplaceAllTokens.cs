using ScsHelixLayerGenerator.Data.Properties;
using ScsHelixLayerGenerator.Data.Properties.Collectors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScsHelixLayerGenerator.Pipelines.HelixLayerGenerator
{
	public class ReplaceAllTokens
	{
		public void Process(HelixLayerGeneratorArgs args)
		{

			Stack<string> directories = new Stack<string>(args.NewLayerRoots);
			while(directories.Count > 0)
			{
				var directory = directories.Pop();
				directory = ProcessName(directory, args.Properties);
				foreach (var file in Directory.GetFiles(directory))
				{
					ProcessFile(file, args.Properties);
					ProcessName(file, args.Properties);
				}
				foreach (var childDirectory in Directory.GetDirectories(directory))
				{
					directories.Push(childDirectory);
				}
			}
			for (int i = 0; i < args.NewLayerRoots.Count; i++)
			{
				args.NewLayerRoots[i] = ReplaceTokens(args.NewLayerRoots[i], args.Properties);
			}
		}
		private static string ProcessName(string path, Dictionary<string, DefaultCollector> properties)
		{
			var resolved = ReplaceTokens(path, properties);
			if (resolved != path)
			{
				try
				{
					Directory.Move(path, resolved);
				}
				catch (IOException e)
				{
					throw new IOException($"Problem renaming file {path}.  This is likely due to another project with the same name, remove the old project and try again.", e);
				}
			}
			return resolved;
		}
		private static void ProcessFile(string file, Dictionary<string, DefaultCollector> properties)
		{
			var txt = File.ReadAllText(file);
			try
			{
				File.WriteAllText(file, ReplaceTokens(txt, properties));
			}catch(IOException e)
			{
				throw new IOException($"Unable to write to {file}", e);
			}
		}
		public static string ReplaceTokens(string text, Dictionary<string, DefaultCollector> properties)
		{
			StringBuilder sb = new StringBuilder(text);
			bool token = false;
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
						if (properties.ContainsKey(tmp))
						{
							sb.Remove(i, tmp.Length);
							sb.Insert(i, properties[tmp].Value);
						}
						tokenName.Clear();
					}
					token = !token;
				}else if (!char.IsUpper(text[i]))
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
