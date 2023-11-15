using System;
using System.Windows.Forms;

namespace AssetStudio.GUI
{
    public partial class MainForm
    {
        public ListView AssetListView => this.assetListView;

        public MenuStrip MenuStrip => this.menuStrip1;

        public void OpenFolderDelegate(Action<OpenFolderDialog> okAction)
        {
            var openFolderDialog = new OpenFolderDialog();
            openFolderDialog.InitialFolder = openDirectoryBackup;
            if (openFolderDialog.ShowDialog(this) == DialogResult.OK)
            {
                openDirectoryBackup = openFolderDialog.Folder;
                okAction?.Invoke(openFolderDialog);
            }
        }
        public void OpenFolderDelegate(Action<OpenFolderDialog> okAction,string title)
        {
            var openFolderDialog = new OpenFolderDialog();
            openFolderDialog.Title = title;
            openFolderDialog.InitialFolder = openDirectoryBackup;
            if (openFolderDialog.ShowDialog(this) == DialogResult.OK)
            {
                openDirectoryBackup = openFolderDialog.Folder;
                okAction?.Invoke(openFolderDialog);
            }
        }


        public AssetsManager CreateAssetsManager()
        {
            AssetsManager assetsManager = new AssetsManager();
            assetsManager.Game = Studio.Game;
            assetsManager.SpecifyUnityVersion = specifyUnityVersion.Text;
            return assetsManager;
        }
    }
}