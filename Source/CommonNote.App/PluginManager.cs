﻿using CommonNote.PluginInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace CommonNote
{
	public static class PluginManager
	{
		private static List<ICommonNoteProvider> _provider = new List<ICommonNoteProvider>();
		public static IEnumerable<ICommonNoteProvider> LoadedPlugins { get { return _provider; } }

		public static void LoadPlugins()
		{
			_provider = new List<ICommonNoteProvider>();

			var pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"plugins\");
			var pluginfiles = Directory.GetFiles(pluginPath, "*.dll");

			foreach (var path in pluginfiles)
			{
				if (path.Contains("StandardNote")) continue; //TODO ONLY FOR DEBUGGING !! 

				try
				{
					LoadPlugin(path);
				}
				catch (ReflectionTypeLoadException e)
				{
					MessageBox.Show("Could not load plugin from " + path + "\r\n\r\n" + e + "\r\n\r\n" + string.Join("\r\n--------\r\n", e.LoaderExceptions.Select(p => p.ToString())), "Could not load plugin");
				}
				catch (Exception e)
				{
					MessageBox.Show("Could not load plugin from " + path + "\r\n\r\n" + e, "Could not load plugin");
				}
			}
		}

		private static void LoadPlugin(string path)
		{
			AssemblyName an = AssemblyName.GetAssemblyName(path);
			Assembly assembly = Assembly.Load(an);
			if (assembly == null) throw new Exception("Could not load assembly '" + an.FullName + "'");

			if (assembly != null)
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (type.IsInterface || type.IsAbstract) continue;

					if (type.GetInterface(typeof(ICommonNoteProvider).FullName) != null)
					{
						ICommonNoteProvider instance = (ICommonNoteProvider)Activator.CreateInstance(type);

						if (instance == null) throw new Exception("Could not instantiate ICommonNotePlugin '" + type.FullName + "'");

						_provider.Add(instance);
					}
				}
			}
		}

		public static ICommonNoteProvider GetDefaultPlugin()
		{
			foreach (var plugin in LoadedPlugins)
			{
				if (plugin.GetUniqueID() == Guid.Parse("37de6de1-26b0-41f5-b252-5e625d9ecfa3")) return plugin; // Local Storage
			}

			return LoadedPlugins.First();
		}

		public static ICommonNoteProvider GetPlugin(Guid uuid)
		{
			return LoadedPlugins.FirstOrDefault(p => p.GetUniqueID() == uuid);
		}
	}
}
