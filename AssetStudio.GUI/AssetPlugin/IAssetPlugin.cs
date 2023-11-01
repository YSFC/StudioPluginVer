using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetStudio.GUI
{

    public enum RegisterMenuType
    {
        None,
        Command,
        Folder,
    }

    public interface IAssetPlugin
    {
        string RegisterMenuText { get; }

        string Name {  get; }
        RegisterMenuType RegisterMenuType { get; }
        void Run(PluginConnection connection);
    }
}
