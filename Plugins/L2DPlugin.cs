using AssetStudio.GUI;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using UnityLive2DExtractor;

namespace Plugins.Ahykal
{
    public class L2DPlugin : IAssetPlugin
    {
        public string RegisterMenuText => "Live2DTools";

        public string Name => "Ahykal.Plugin";

        public RegisterMenuType RegisterMenuType => RegisterMenuType.Folder;

        public void Run(PluginConnection connection)
        {
            this.SetCommand(new ToolStripMenuItem("ExportNormal")).Click += delegate
            {
                connection.Mainform.OpenFolderDelegate(async o =>
                {
                    var assetsManager = connection.Mainform.CreateAssetsManager();
                    await Task.Run(() => Live2DExtractor.Extract(assetsManager, o.Folder));
                    await Console.Out.WriteLineAsync("exit");
                });
            };
            this.SetCommand(new ToolStripMenuItem("SaveMotionsByAnimators")).Click += delegate
            {
                connection.Mainform.OpenFolderDelegate(o =>
                {
                    Util.SaveMotions(o.Folder, true);
                });
            };
            this.SetCommand(new ToolStripMenuItem("SavePhysics")).Click += delegate
            {
                connection.Mainform.OpenFolderDelegate(o =>
                {
                    Util.SaveMP(Util.MPType.GetPhysicByClassName, o.Folder, true);
                });
            };
            this.SetCommand(new ToolStripMenuItem("SavePhysics(2)")).Click += delegate
            {
                connection.Mainform.OpenFolderDelegate(o =>
                {
                    Util.SaveMP(Util.MPType.GetPhysicByClassName | Util.MPType.GetPhysicBySubRig, o.Folder, true);
                });
            };
            this.SetCommand(new ToolStripMenuItem("SaveMoc3")).Click += delegate
            {
                connection.Mainform.OpenFolderDelegate(o =>
                {
                    Util.SaveMP(Util.MPType.GetMocByClassName | Util.MPType.GetMocByHeader, o.Folder, true);
                });
            };

            this.SetCommand(new ToolStripMenuItem("GetL2DByAnimator")).Click += delegate
            {

                connection.Mainform.OpenFolderDelegate(o =>
                {
                    Util.GetL2DByAnimator(o.Folder,false);
                });
                
            };

            this.SetCommand(new ToolStripMenuItem("GetL2DByAnimator(motionNameWithIndex)")).Click += delegate
            {

                connection.Mainform.OpenFolderDelegate(o =>
                {
                    Util.GetL2DByAnimator(o.Folder, true);
                });

            };
        }
    }
}