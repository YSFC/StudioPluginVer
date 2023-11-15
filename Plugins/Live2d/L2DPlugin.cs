using AssetStudio;
using AssetStudio.GUI;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using UnityLive2DExtractor;

namespace Plugins.Ahykal
{
    public class L2DPlugin : IAssetPlugin
    {
        public string Name => "Ahykal.Live2D";
        public string RegisterMenuText => "Live2D";
        public RegisterMenuType RegisterMenuType => RegisterMenuType.Folder;

        public void Run(PluginConnection connection)
        {
            this.SetCommand(new ToolStripMenuItem("ExtractFromFolder")).Click += delegate
            {
                connection.Mainform.OpenFolderDelegate(async o =>
                {
                    var assetsManager = connection.Mainform.CreateAssetsManager();
                    await Task.Run(() => Live2DExtractor.Extract(assetsManager, o.Folder));
                    await Console.Out.WriteLineAsync("Done!");
                });
            };
            this.SetCommand(new ToolStripSeparator());
            var withPathIDCheck = new ToolStripMenuItem("ExportWithPathIDFolder") { CheckOnClick = true, Checked = true };
            this.SetCommand(withPathIDCheck);

            this.SetCommand(new ToolStripMenuItem("SaveByAllAnimators")).Click += delegate
            {
                connection.Mainform.OpenFolderDelegate(o =>
                {
                    Live2DExtractor.SaveByAnimators(AssetList.ExportableAssets.Where(x => x.Type == ClassIDType.Animator), o.Folder, withPathIDCheck.Checked);
                });
            };
            this.SetCommand(new ToolStripMenuItem("SaveBySelectedAnimators")).Click += delegate
            {
                connection.Mainform.OpenFolderDelegate(o =>
                {
                    Live2DExtractor.SaveByAnimators(AssetList.GetSelectedAssets().Where(x => x.Type == ClassIDType.Animator), o.Folder, withPathIDCheck.Checked);
                });
            };
            this.SetCommand(new ToolStripMenuItem("SaveMotionsBySelected") { ToolTipText = "Select target Animators&AnimationClips then click." }).Click += delegate
            {
                connection.Mainform.OpenFolderDelegate(o =>
                {
                    Live2DExtractor.SaveMotionsBySelectedAnimatorClips(o.Folder, withPathIDCheck.Checked);
                });
            };

            this.SetCommand(new ToolStripMenuItem("ForceSaveAsMoc3") { ToolTipText = "Select target MonoBehaviour then click." }).Click += delegate
            {
                connection.Mainform.OpenFolderDelegate(o =>
                {
                    Live2DExtractor.ForceSaveAsMoc3(AssetList.GetSelectedAssets(), o.Folder, withPathIDCheck.Checked);
                });
            };

            this.SetCommand(new ToolStripMenuItem("ForceSaveAsPhysics") { ToolTipText = "Select target MonoBehaviour then click." }).Click += delegate
            {
                connection.Mainform.OpenFolderDelegate(o =>
                {
                    Live2DExtractor.ForceSaveAsPhysics(AssetList.GetSelectedAssets(), o.Folder, withPathIDCheck.Checked);
                });
            };
        }
    }
}