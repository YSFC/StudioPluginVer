using System;
using System.Collections.Generic;
using System.Linq;
using static AssetStudio.GUI.PluginConnection;

namespace AssetStudio.GUI
{
    public static class AssetList
    {
        public static List<AssetItem> ExportableAssets { get => Studio.exportableAssets; }

        public static List<AssetItem> VisibleAssets
        {
            get => Studio.visibleAssets;
            set
            {
                instance.Mainform.AssetListView.BeginUpdate();
                instance.Mainform.AssetListView.SelectedIndices.Clear();
                Studio.visibleAssets = value;
                instance.Mainform.AssetListView.VirtualListSize = VisibleAssets.Count;
                instance.Mainform.AssetListView.EndUpdate();
            }
        }
        public static IEnumerable<AssetItem> GetSelectedAssets()
        {
            foreach (int index in instance.Mainform.AssetListView.SelectedIndices)
            {
                yield return (AssetItem)instance.Mainform.AssetListView.Items[index];
            }
        }

        public static IEnumerable<AssetItem> GetUnSelectedAssets()
        {
            return VisibleAssets.Where(x => x.Selected == false);
        }

        public static void SelectInSameFile()
        {
            var temp = GetSelectedAssets().Select(x => x.Asset.assetsFile.originalPath);
            VisibleAssets.Where(x => temp.Contains(x.Asset.assetsFile.originalPath))
                .Apply(x => x.Selected = true);
        }

        public static void SelectReverse()
        {
            VisibleAssets.ForEach(x => x.Selected = !x.Selected);
        }
    }
}