using System;
using System.Collections.Generic;
using uTinyRipper.AssetExporters;
using uTinyRipper.YAML;
using uTinyRipper.SerializedFiles;

namespace uTinyRipper.Classes
{
	public class Transform : Component
	{
		public Transform(AssetInfo assetInfo) :
			base(assetInfo)
		{
		}

		public override void Read(AssetReader reader)
		{
			base.Read(reader);
			
			LocalRotation.Read(reader);
			LocalPosition.Read(reader);
			LocalScale.Read(reader);
			m_children = reader.ReadAssetArray<PPtr<Transform>>();
			Father.Read(reader);
		}

		public override IEnumerable<Object> FetchDependencies(ISerializedFile file, bool isLog = false)
		{
			foreach(Object asset in base.FetchDependencies(file, isLog))
			{
				yield return asset;
			}

			foreach (PPtr<Transform> ptr in Children)
			{
				yield return ptr.GetAsset(file);
			}
			if (!Father.IsNull)
			{
				yield return Father.GetAsset(file);
			}
		}

		public string GetRootPath()
		{
			string pre = string.Empty;
			if(!Father.IsNull)
			{
				pre = Father.GetAsset(File).GetRootPath() + "/";
			}
			return pre + GameObject.GetAsset(File).Name;
		}

		public int GetSiblingIndex()
		{
			if(Father.IsNull)
			{
				return 0;
			}
			Transform father = Father.GetAsset(File);
			for(int i = 0; i < father.Children.Count; i++)
			{
				PPtr<Transform> child = father.Children[i];
				if (child.PathID == PathID)
				{
					return i;
				}
			}
			throw new Exception("Transorm hasn't been found among father's children");
		}

		public Transform FindChild(string path)
		{
			if (path == string.Empty)
			{
				return this;
			}
			return FindChild(path, 0);
		}

		protected override YAMLMappingNode ExportYAMLRoot(IExportContainer container)
		{
			YAMLMappingNode node = base.ExportYAMLRoot(container);
			node.Add(LocalRotationName, LocalRotation.ExportYAML(container));
			node.Add(LocalPositionName, LocalPosition.ExportYAML(container));
			node.Add(LocalScaleName, LocalScale.ExportYAML(container));
			node.Add(ChildrenName, Children.ExportYAML(container));
			node.Add(FatherName, Father.ExportYAML(container));
			node.Add(RootOrderName, GetSiblingIndex());
			node.Add(LocalEulerAnglesHintName, LocalRotation.ToEuler().ExportYAML(container));
			return node;
		}

		private Transform FindChild(string path, int startIndex)
		{
			int separatorIndex = path.IndexOf(PathSeparator, startIndex);
			string childName = separatorIndex == -1 ?
				path.Substring(startIndex, path.Length - startIndex) :
				path.Substring(startIndex, separatorIndex - startIndex);
			foreach (PPtr<Transform> childPtr in m_children)
			{
				Transform child = childPtr.GetAsset(File);
				GameObject childGO = child.GameObject.GetAsset(File);
				if (childGO.Name == childName)
				{
					return separatorIndex == -1 ? child : child.FindChild(path, separatorIndex + 1);
				}
			}
			return null;
		}

		public const string LocalRotationName = "m_LocalRotation";
		public const string LocalPositionName = "m_LocalPosition";
		public const string LocalScaleName = "m_LocalScale";
		public const string ChildrenName = "m_Children";
		public const string FatherName = "m_Father";
		public const string RootOrderName = "m_RootOrder";
		public const string LocalEulerAnglesHintName = "m_LocalEulerAnglesHint";

		public IReadOnlyList<PPtr<Transform>> Children => m_children;

		public const char PathSeparator = '/';

		public Quaternionf LocalRotation;
		public Vector3f LocalPosition;
		public Vector3f LocalScale;
		public PPtr<Transform> Father;

		private PPtr<Transform>[] m_children;
	}
}
