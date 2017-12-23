﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using AlephNote.Common.Plugins;
using AlephNote.PluginInterface;
using AlephNote.PluginInterface.Util;

namespace AlephNote.Common.AlephXMLSerialization
{
	public class AXMLFieldInfo
	{
		public enum SettingObjectTypeEnum
		{
			Integer,
			Double,
			NullableInteger,
			Boolean,
			Guid,
			NGuid,
			EncryptedString,
			String,
			Enum,

			DirectoryPath,
			RemoteStorageAccount,
			ListRemoteStorageAccount,

			CustomSerializable,
		}

		private readonly SettingObjectTypeEnum _objectType;
		public readonly PropertyInfo PropInfo;
		
		public AXMLFieldInfo(SettingObjectTypeEnum t, PropertyInfo i)
		{
			_objectType = t;
			PropInfo = i;
		}

		public XElement Serialize(object objdata)
		{
			string resultdata;

			switch (_objectType)
			{
				case SettingObjectTypeEnum.Integer:
					resultdata = Convert.ToString((int)objdata);
					break;

				case SettingObjectTypeEnum.NullableInteger:
					var nint = (int?)objdata;
					resultdata = nint == null ? string.Empty : nint.ToString();
					break;

				case SettingObjectTypeEnum.Boolean:
					resultdata = Convert.ToString((bool)objdata);
					break;

				case SettingObjectTypeEnum.Guid:
					resultdata = ((Guid)objdata).ToString("B");
					break;

				case SettingObjectTypeEnum.NGuid:
					resultdata = ((Guid?)objdata)?.ToString("B") ?? "";
					break;

				case SettingObjectTypeEnum.EncryptedString:
					resultdata = AlephXMLSerializerHelper.Encrypt((string)objdata);
					break;

				case SettingObjectTypeEnum.String:
					resultdata = (string)objdata;
					break;

				case SettingObjectTypeEnum.Double:
					resultdata = ((double)objdata).ToString("R");
					break;

				case SettingObjectTypeEnum.Enum:
					resultdata = Convert.ToString(objdata);
					break;

				case SettingObjectTypeEnum.RemoteStorageAccount:
					resultdata = ((RemoteStorageAccount)objdata).ID.ToString("B");
					break;

				case SettingObjectTypeEnum.ListRemoteStorageAccount:
					var x1 = new XElement(PropInfo.Name);
					x1.Add(new XAttribute("type", SettingObjectTypeEnum.ListRemoteStorageAccount));
					x1.Add(((IList<RemoteStorageAccount>)objdata).Select(SerializeRemoteStorageAccount));
					return x1;

				case SettingObjectTypeEnum.CustomSerializable:
					var x2 = new XElement(PropInfo.Name);
					var d = ((IAlephCustomSerializableField)objdata);
					x2.Add(new XAttribute("type", d.GetTypeStr()));
					d.Serialize(x2);
					return x2;

				case SettingObjectTypeEnum.DirectoryPath:
					return new XElement(PropInfo.Name, ((DirectoryPath)objdata).Serialize(), new XAttribute("type", _objectType));

				default:
					throw new ArgumentOutOfRangeException(nameof(objdata), _objectType, null);
			}

			return new XElement(PropInfo.Name, resultdata, new XAttribute("type", _objectType));
		}

		public void Deserialize(object obj, XElement root)
		{
			var current = PropInfo.GetValue(obj);

			switch (_objectType)
			{
				case SettingObjectTypeEnum.Integer:
					PropInfo.SetValue(obj, XHelper.GetChildValue(root, PropInfo.Name, (int)current));
					return;

				case SettingObjectTypeEnum.Double:
					PropInfo.SetValue(obj, XHelper.GetChildValue(root, PropInfo.Name, (double)current));
					return;

				case SettingObjectTypeEnum.NullableInteger:
					PropInfo.SetValue(obj, XHelper.GetChildValue(root, PropInfo.Name, (int?)current));
					return;

				case SettingObjectTypeEnum.Boolean:
					PropInfo.SetValue(obj, XHelper.GetChildValue(root, PropInfo.Name, (bool)current));
					return;

				case SettingObjectTypeEnum.Guid:
					PropInfo.SetValue(obj, XHelper.GetChildValue(root, PropInfo.Name, (Guid)current));
					return;

				case SettingObjectTypeEnum.NGuid:
					PropInfo.SetValue(obj, XHelper.GetChildValue(root, PropInfo.Name, (Guid?)current));
					return;

				case SettingObjectTypeEnum.EncryptedString:
					PropInfo.SetValue(obj, AlephXMLSerializerHelper.Decrypt(XHelper.GetChildValue(root, PropInfo.Name, AlephXMLSerializerHelper.Encrypt((string)current))));
					return;

				case SettingObjectTypeEnum.String:
					PropInfo.SetValue(obj, XHelper.GetChildValue(root, PropInfo.Name, (string)current));
					return;

				case SettingObjectTypeEnum.Enum:
					PropInfo.SetValue(obj, XHelper.GetChildValue(root, PropInfo.Name, current, PropInfo.PropertyType));
					break;

				case SettingObjectTypeEnum.RemoteStorageAccount:
					var currUUID = ((RemoteStorageAccount)current).ID;
					PropInfo.SetValue(obj, new RemoteStorageAccount(XHelper.GetChildValue(root, PropInfo.Name, currUUID), null, null));
					break;

				case SettingObjectTypeEnum.ListRemoteStorageAccount:
					var list = (IList<RemoteStorageAccount>)current;
					var child = XHelper.GetChildOrNull(root, PropInfo.Name);
					if (child != null)
					{
						list.Clear();
						foreach (var elem in XHelper.GetChildOrThrow(root, PropInfo.Name).Elements())
						{
							list.Add(DeserializeRemoteStorageAccount(elem));
						}
					}
					break;

				case SettingObjectTypeEnum.CustomSerializable:
					var currCust = ((IAlephCustomSerializableField)current);
					var cchild = XHelper.GetChildOrNull(root, PropInfo.Name);
					if (cchild != null) PropInfo.SetValue(obj, currCust.DeserializeNew(cchild));
					break;

				case SettingObjectTypeEnum.DirectoryPath:
					var dp = DirectoryPath.Deserialize(XHelper.GetChildrenOrEmpty(root, PropInfo.Name, "PathComponent"));
					PropInfo.SetValue(obj, dp);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(obj), _objectType, null);
			}
		}

		private XElement SerializeRemoteStorageAccount(RemoteStorageAccount rsa)
		{
			var x = new XElement("Account");
			x.Add(new XAttribute("type", SettingObjectTypeEnum.RemoteStorageAccount));
			x.Add(new XElement("ID", rsa.ID.ToString("B"), new XAttribute("type", SettingObjectTypeEnum.Guid)));
			x.Add(new XElement("Plugin", rsa.Plugin.GetUniqueID().ToString("B"), new XAttribute("type", SettingObjectTypeEnum.Guid)));
			x.Add(new XElement("Config", rsa.Config.Serialize(), new XAttribute("type", "Generic")));
			return x;
		}

		private RemoteStorageAccount DeserializeRemoteStorageAccount(XElement e)
		{
			var rsa = new RemoteStorageAccount();
			rsa.ID = XHelper.GetChildValue(e, "ID", Guid.Empty);
			rsa.Plugin = PluginManagerSingleton.Inst.GetPlugin(XHelper.GetChildValue(e, "Plugin", Guid.Empty));
			rsa.Config = rsa.Plugin.CreateEmptyRemoteStorageConfiguration();
			rsa.Config.Deserialize(XHelper.GetChildOrThrow(e, "Config").Elements().Single());
			return rsa;
		}
	}


}
