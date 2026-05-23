using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public class AssetInfo
    {
        public int preloadIndex;
        public int preloadSize;
        public PPtr<Object> asset;

        public AssetInfo(ObjectReader reader)
		{
			if (reader.Game.Type.IsEndlessDream())
			{
				if (reader.m_Version < SerializedFileFormatVersion.Unknown_14)
				{
					reader.ReadInt32();
				}
				else
				{
					reader.ReadInt64();
				}
			}
			preloadIndex = reader.ReadInt32();
            preloadSize = reader.ReadInt32();
            asset = new PPtr<Object>(reader);
        }
    }

    public sealed class AssetBundle : NamedObject
    {
        public List<PPtr<Object>> m_PreloadTable;
        public List<KeyValuePair<string, AssetInfo>> m_Container;

        public AssetBundle(ObjectReader reader) : base(reader)
        {
       
            var m_PreloadTableSize = reader.ReadInt32();
            m_PreloadTable = new List<PPtr<Object>>();
            for (int i = 0; i < m_PreloadTableSize; i++)
            {
                m_PreloadTable.Add(new PPtr<Object>(reader));
            }

            var m_ContainerSize = reader.ReadInt32();
            m_Container = new List<KeyValuePair<string, AssetInfo>>();
            for (int i = 0; i < m_ContainerSize; i++)
            {
                if (reader.Game.Type.IsEndlessDream())
                {
                    var assetInfo = new AssetInfo(reader);
                    var container = reader.ReadAlignedString();
                    m_Container.Add(new KeyValuePair<string, AssetInfo>(container, assetInfo));
                }
                else
                {
                    m_Container.Add(new KeyValuePair<string, AssetInfo>(reader.ReadAlignedString(), new AssetInfo(reader)));
                }
            }
        }
    }
}
