using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace AssetStudio.GUI
{
    public class PluginInfo
    {
        internal ToolStripMenuItem menuItem_Folder;

        static PluginInfo()
        {
            Plugins = new Dictionary<IAssetPlugin, PluginInfo>();
        }

        internal PluginInfo(string path, string cls, string subFolder = "")
        {
            this.AssemblyPath = path;
            this.ClassName = cls;
            this.SubFolder = subFolder;
        }

        public static Dictionary<IAssetPlugin, PluginInfo> Plugins { get; private set; }
        public string AssemblyPath { get; protected set; }
        public string ClassName { get; protected set; }
        public IAssetPlugin Module { get; private set; }
        public string SubFolder { get; protected set; }

        public static void FindPlugins(string pluginFolder)
        {
            var folderPath = Path.Combine(pluginFolder, "Plugins");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            var dllfiles = Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories);
            foreach (var dllfile in dllfiles)
            {
                Assembly assembly = Assembly.LoadFrom(dllfile);
                string text = Path.GetDirectoryName(dllfile);
                //text = text.Replace(folderPath + "\\", "");
                text = text.Replace(folderPath, "");
                Type[] types = assembly.GetTypes();

                foreach (Type type in types)
                {
                    if (type.IsInterface)
                    {
                        continue;
                    }
                    if (type.IsAbstract)
                    {
                        continue;
                    }
                    if (type.GetInterfaces().Contains(typeof(IAssetPlugin)))
                    {
                        PluginInfo pluginInfo = new PluginInfo(dllfile, type.FullName, text);
                        pluginInfo.CreatePluginInstance();
                        Plugins[pluginInfo.Module] = pluginInfo;
                    }
                }
            }
        }

        public static PluginInfo GetPluginInfo(IAssetPlugin plugin)
        {
            return Plugins[plugin];
        }

        internal static void RegisterMenu(PluginConnection instance)
        {
            Dictionary<PluginInfo, ToolStripMenuItem> dictionary = CreateSubMenus(instance.MenuItem_Plugin, 0);
            if (dictionary == null)
            {
                return;
            }
            int num = 0;
            foreach (var kv in Plugins)
            {
                PluginInfo pluginInfo = kv.Value;
                if (dictionary.ContainsKey(pluginInfo))
                {
                    IAssetPlugin j = pluginInfo.Module;
                    if (j.RegisterMenuType != RegisterMenuType.None)
                    {
                        string text;
                        if (!string.IsNullOrEmpty(j.RegisterMenuText))
                        {
                            text = j.RegisterMenuText;
                        }
                        else
                        {
                            text = j.Name;
                        }
                        string dllPath2 = pluginInfo.AssemblyPath;
                        ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem();
                        ToolStripItem toolStripItem = toolStripMenuItem;
                        string text2 = "MenuItem_Plugin";
                        int num2 = num;
                        num = num2 + 1;
                        toolStripItem.Name = text2 + num2.ToString();
                        toolStripMenuItem.AutoSize = true;
                        toolStripMenuItem.Text = text;

                        if (j.RegisterMenuType == RegisterMenuType.Command)
                        {
                            dictionary[pluginInfo].DropDownItems.Add(toolStripMenuItem);
                            toolStripMenuItem.Click += delegate (object sender, EventArgs e)
                            {
                                RunPlugin(j, instance);
                            };
                        }
                        else
                        {
                            pluginInfo.menuItem_Folder = toolStripMenuItem;
                            RunPlugin(j, instance);
                            if (pluginInfo.menuItem_Folder.HasDropDownItems)
                            {
                                dictionary[pluginInfo].DropDownItems.Add(pluginInfo.menuItem_Folder);
                            }
                        }
                    }
                    else
                    {
                        RunPlugin(j, instance);
                    }
                }
            }
        }

        public static void RunPlugin(IAssetPlugin plugin, PluginConnection connection)
        {
            try
            {
                plugin.Run(connection);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        internal void CreatePluginInstance()
        {
            if (Assembly.LoadFrom(AssemblyPath).CreateInstance(ClassName) is IAssetPlugin plugin)
            {
                this.Module = plugin;
            }
        }

        public void SetCommand(ToolStripItem toolStripItem)
        {
            if (menuItem_Folder != null)
            {
                menuItem_Folder.DropDownItems.Add(toolStripItem);
            }
        }

        private static ToolStripMenuItem ContainsMenuText(ToolStripMenuItem menu, string text)
        {
            int count = menu.DropDownItems.Count;
            for (int i = 0; i < count; i++)
            {
                if (menu.DropDownItems[i].Text == text)
                {
                    return (ToolStripMenuItem)menu.DropDownItems[i];
                }
            }
            return null;
        }

        private static Dictionary<PluginInfo, ToolStripMenuItem> CreateSubMenus(ToolStripMenuItem rootMenu, int stCount = 0)
        {
            Dictionary<PluginInfo, ToolStripMenuItem> dictionary = new Dictionary<PluginInfo, ToolStripMenuItem>();
            foreach (var pluginInfoBase in Plugins.Values)
            {
                if (pluginInfoBase.SubFolder == "")
                {
                    dictionary.Add(pluginInfoBase, rootMenu);
                }
                else
                {
                    string[] array = pluginInfoBase.SubFolder.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    ToolStripMenuItem toolStripMenuItem = rootMenu;
                    foreach (string text in array)
                    {
                        ToolStripMenuItem toolStripMenuItem2 = ContainsMenuText(toolStripMenuItem, text);
                        if (toolStripMenuItem2 == null)
                        {
                            ToolStripMenuItem toolStripMenuItem3 = new ToolStripMenuItem();
                            ToolStripItem toolStripItem = toolStripMenuItem3;
                            string text2 = "MenuItem_Plugin_SubItems";
                            int num = stCount;
                            stCount = num + 1;
                            toolStripItem.Name = text2 + num.ToString();
                            toolStripMenuItem3.AutoSize = true;
                            toolStripMenuItem3.Text = text;
                            toolStripMenuItem.DropDownItems.Add(toolStripMenuItem3);
                            toolStripMenuItem2 = toolStripMenuItem3;
                        }
                        toolStripMenuItem = toolStripMenuItem2;
                    }
                    dictionary.Add(pluginInfoBase, toolStripMenuItem);
                }
            }
            return dictionary;
        }
    }
}