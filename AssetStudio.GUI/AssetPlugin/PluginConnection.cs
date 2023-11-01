using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AssetStudio.GUI
{
    public class PluginConnection
    {
        internal static readonly PluginConnection instance = new();

        private PluginConnection(){ }

        public MainForm Mainform { get; private set; }
        public ToolStripMenuItem MenuItem_Plugin { get; private set; }


        internal static void SetMainForm(MainForm mainForm)
        {
            instance.Mainform = mainForm;
            instance.MenuItem_Plugin = new ToolStripMenuItem("Plugin");
            mainForm.MenuStrip.Items.Add(instance.MenuItem_Plugin);
        }

        internal static void InitPlugin()
        {
            PluginInfo.FindPlugins(Application.StartupPath);
            PluginInfo.RegisterMenu(instance);
        }
    }
}