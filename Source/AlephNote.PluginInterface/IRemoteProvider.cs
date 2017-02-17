﻿using System;
using System.Net;

namespace AlephNote.PluginInterface
{
	public interface IRemoteProvider
	{
		string DisplayTitleLong { get; }
		string DisplayTitleShort { get; }

		Guid GetUniqueID();
		string GetName();
		Version GetVersion();

		IRemoteStorageConfiguration CreateEmptyRemoteStorageConfiguration();
		IRemoteStorageConnection CreateRemoteStorageConnection(IWebProxy proxy, IRemoteStorageConfiguration config);
		INote CreateEmptyNote(IRemoteStorageConfiguration cfg);
	}

	public abstract class RemoteBasicProvider : IRemoteProvider
	{
		private readonly Guid uuid;
		private readonly string name;
		private readonly Version version;

		public string DisplayTitleLong { get { return GetName() + " v" + GetVersion(); } }
		public string DisplayTitleShort { get { return GetName(); } }

		protected RemoteBasicProvider(string name, Version version, Guid uuid)
		{
			this.name = name;
			this.uuid = uuid;
			this.version = version;
		}

		public Guid GetUniqueID()
		{
			return uuid;
		}

		public string GetName()
		{
			return name;
		}

		public Version GetVersion()
		{
			return version;
		}

		public abstract IRemoteStorageConfiguration CreateEmptyRemoteStorageConfiguration();
		public abstract IRemoteStorageConnection CreateRemoteStorageConnection(IWebProxy proxy, IRemoteStorageConfiguration config);
		public abstract INote CreateEmptyNote(IRemoteStorageConfiguration cfg);
	}
}
