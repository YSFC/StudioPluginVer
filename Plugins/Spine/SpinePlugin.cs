using AssetStudio;
using AssetStudio.GUI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Plugins.Ahykal
{
    public class SpinePlugin : IAssetPlugin
    {
        public string Name => "Ahykal.Spine";
        public string RegisterMenuText => "Spine";
        public RegisterMenuType RegisterMenuType => RegisterMenuType.Folder;

        public static void SaveByMonoBehaviourScript(IEnumerable<AssetItem> monoBehaviours, string folder, bool withSkel, bool withPathID)
        {
            monoBehaviours.Apply(asset =>
            {
                var monoBehaviour = asset.Asset as MonoBehaviour;
                if (monoBehaviour.m_Script.TryGet(out MonoScript script) && script.Name == "SkeletonDataAsset")
                {
                    if (monoBehaviour.TryGetComponent("skeletonJSON", out TextAsset skel))
                    {
                        #region skelName

                        string skelName;
                        if (skel.Name.EndsWith(".skel") || skel.Name.EndsWith(".json"))
                        {
                            skelName = skel.Name;
                        }
                        else
                        {
                            skelName = Path.GetFileNameWithoutExtension(skel.Name) + ".skel";
                        }

                        #endregion skelName

                        #region subfolder

                        string subfolder = folder;
                        if (withSkel)
                        {
                            subfolder = Path.Combine(subfolder, Path.GetFileNameWithoutExtension(skelName));
                        }
                        if (withPathID)
                        {
                            subfolder = Path.Combine(subfolder, skel.m_PathID.ToString());
                        }
                        Directory.CreateDirectory(subfolder);

                        #endregion subfolder

                        #region saveSkel

                        string skelPath = Path.Combine(subfolder, skelName);
                        File.WriteAllBytes(skelPath, skel.m_Script);
                        Console.WriteLine($"------\nSave {skelPath} successfully.");

                        #endregion saveSkel

                        if (monoBehaviour.TryGetComponents("atlasAssets", out List<MonoBehaviour> atalsTargets))
                        {
                            atalsTargets.ForEach(atalsTarget =>
                            {
                                if (atalsTarget.TryGetComponent("atlasFile", out TextAsset atlas))
                                {
                                    #region saveAtlas

                                    string atlasName;
                                    if (atlas.Name.EndsWith(".atlas"))
                                    {
                                        atlasName = atlas.Name;
                                    }
                                    else
                                    {
                                        atlasName = Path.GetFileNameWithoutExtension(atlas.Name) + ".atlas";
                                    }
                                    string atlasPath = Path.Combine(subfolder, atlasName);
                                    File.WriteAllBytes(atlasPath, atlas.m_Script);
                                    Console.WriteLine($"Save {atlasPath} successfully.");

                                    #endregion saveAtlas
                                }
                                if (atalsTarget.TryGetComponents("materials", out List<Material> materials))
                                {
                                    var textures = materials.ApplyFunc(material => material.m_SavedProperties.m_TexEnvs[0].Value.m_Texture.TryGet(out Texture2D texture2D) ? texture2D : null, true);
                                    textures.SaveTextures(subfolder);
                                    //SaveTextures(textures, subfolder);
                                }
                            });
                        }
                    }
                }
            });
        }

        public static void SaveByMonoBehaviourScript(IEnumerable<AssetItem> monoBehaviours, string folder, bool withSkel, bool withPathID, float scale, int anisoLevel, float posX, float posY, bool withEdgePadding, bool withShader)
        {
            JObject configOptions = new JObject();
            configOptions["scale_factor"] = scale;
            configOptions["aniso_level"] = anisoLevel;
            configOptions["position_x"] = posX;
            configOptions["position_y"] = posY;
            if (!withEdgePadding)
            {
                configOptions["edge_padding"] = false;
            }
            if (withShader)
            {
                configOptions["shader"] = "Skeleton-Straight-Alpha";
            }
            monoBehaviours.Apply(asset =>
            {
                var monoBehaviour = asset.Asset as MonoBehaviour;
                if (monoBehaviour.m_Script.TryGet(out MonoScript script)&&script.Name == "SkeletonDataAsset")
                {
                    if (monoBehaviour.TryGetComponent("skeletonJSON", out TextAsset skel))
                    {
                        #region skelName

                        string skelName;
                        if (skel.Name.EndsWith(".skel") || skel.Name.EndsWith(".json"))
                        {
                            skelName = skel.Name;
                        }
                        else
                        {
                            skelName = Path.GetFileNameWithoutExtension(skel.Name) + ".skel";
                        }

                        #endregion skelName

                        #region subfolder

                        string subfolder = folder;
                        if (withSkel)
                        {
                            subfolder = Path.Combine(subfolder, Path.GetFileNameWithoutExtension(skelName));
                        }
                        if (withPathID)
                        {
                            subfolder = Path.Combine(subfolder, skel.m_PathID.ToString());
                        }
                        Directory.CreateDirectory(subfolder);

                        #endregion subfolder

                        #region saveSkel

                        string skelPath = Path.Combine(subfolder, skelName);
                        File.WriteAllBytes(skelPath, skel.m_Script);
                        Console.WriteLine($"------\nSave {skelPath} successfully.");
                        JObject configRoot = new JObject();
                        configRoot["skeleton"] = skelName;

                        #endregion saveSkel

                        if (monoBehaviour.TryGetComponents("atlasAssets", out List<MonoBehaviour> atalsTargets))
                        {
                            JArray atlases = new JArray();
                            atalsTargets.ForEach(atalsTarget =>
                            {
                                JObject atlasObject = new JObject();
                                if (atalsTarget.TryGetComponent("atlasFile", out TextAsset atlas))
                                {
                                    #region saveAtlas

                                    string atlasName;
                                    if (atlas.Name.EndsWith(".atlas"))
                                    {
                                        atlasName = atlas.Name;
                                    }
                                    else
                                    {
                                        atlasName = Path.GetFileNameWithoutExtension(atlas.Name) + ".atlas";
                                    }
                                    string atlasPath = Path.Combine(subfolder, atlasName);
                                    File.WriteAllBytes(atlasPath, atlas.m_Script);
                                    Console.WriteLine($"Save {atlasPath} successfully.");
                                    atlasObject["atlas"] = atlasName;

                                    #endregion saveAtlas
                                }
                                if (atalsTarget.TryGetComponents("materials", out List<Material> materials))
                                {
                                    JArray texNames = new JArray();
                                    JArray textures = new JArray();
                                    
                                    var texture2Ds = materials.ApplyFunc(material => material.m_SavedProperties.m_TexEnvs[0].Value.m_Texture.TryGet(out Texture2D texture2D) ? texture2D : null, true);
                                    foreach (var texture2D in texture2Ds)
                                    {
                                        texture2D.SaveTexture(subfolder);
                                        texNames.Add(texture2D.m_Name);
                                        textures.Add(texture2D.m_Name + ".png");
                                    }
                                    atlasObject["tex_names"] = texNames;
                                    atlasObject["textures"] = textures;
                                    atlases.Add(atlasObject);
                                    configRoot["atlases"] = atlases;
                                    configRoot["options"] = configOptions;
                                    string configPath = Path.Combine(subfolder, Path.GetFileNameWithoutExtension(skelName) + ".config.json");
                                    File.WriteAllText(configPath, configRoot.ToString());
                                    Console.WriteLine($"Save {configPath} successfully.");
                                }
                            });
                        }
                    }
                }
            });
            Console.WriteLine("Done!");
        }

        public void Run(PluginConnection connection)
        {
            var withSkelCheck = new ToolStripMenuItem("ExportWithSkelFolder") { CheckOnClick = true, Checked = true };
            this.SetCommand(withSkelCheck);
            var withPathIDCheck = new ToolStripMenuItem("ExportWithPathIDFolder") { CheckOnClick = true, Checked = true };
            this.SetCommand(withPathIDCheck);
            this.SetCommand(new ToolStripSeparator());

            var withExviewerConfigCheck = new ToolStripMenuItem("ExportExviewerConfig") { CheckOnClick = true, Checked = true };
            var scaleTextBox = new ToolStripTextBox() { Text = "1.0", ToolTipText = "Scale" };
            var anisoLevelTextBox = new ToolStripTextBox() { Text = "1", ToolTipText = "AnisoLevel" };
            var posXTextBox = new ToolStripTextBox() { Text = "0", ToolTipText = "PositionX" };
            var posYTextBox = new ToolStripTextBox() { Text = "0", ToolTipText = "PositionY" };
            var edgePaddingCheck = new ToolStripMenuItem("WithEdgePadding") { CheckOnClick = true };
            var shaderCheck = new ToolStripMenuItem("WithShader") { CheckOnClick = true };
            withExviewerConfigCheck.DropDownItems.Add(scaleTextBox);
            withExviewerConfigCheck.DropDownItems.Add(anisoLevelTextBox);
            withExviewerConfigCheck.DropDownItems.Add(posXTextBox);
            withExviewerConfigCheck.DropDownItems.Add(posYTextBox);
            withExviewerConfigCheck.DropDownItems.Add(edgePaddingCheck);
            withExviewerConfigCheck.DropDownItems.Add(shaderCheck);
            this.SetCommand(withExviewerConfigCheck);
            this.SetCommand(new ToolStripSeparator());

            this.SetCommand(new ToolStripMenuItem("SaveByAllMonoBehaviourScript")).Click += delegate
            {
                if (withExviewerConfigCheck.Checked)
                {
                    connection.Mainform.OpenFolderDelegate(o =>
                    {
                        SaveByMonoBehaviourScript(
                            AssetList.ExportableAssets.Where(x => x.Type == ClassIDType.MonoBehaviour),
                            o.Folder,
                            withSkelCheck.Checked,
                            withPathIDCheck.Checked,
                            Convert.ToSingle(scaleTextBox.Text),
                            Convert.ToInt32(anisoLevelTextBox.Text),
                            Convert.ToSingle(posXTextBox.Text),
                            Convert.ToSingle(posYTextBox.Text),
                            edgePaddingCheck.Checked,
                            shaderCheck.Checked
                            );
                    });
                }
                else
                {
                    connection.Mainform.OpenFolderDelegate(o =>
                    {
                        SaveByMonoBehaviourScript(
                            AssetList.ExportableAssets.Where(x => x.Type == ClassIDType.MonoBehaviour),
                            o.Folder,
                            withSkelCheck.Checked,
                            withPathIDCheck.Checked
                            );
                    });
                }
            };
            this.SetCommand(new ToolStripMenuItem("SaveBySelectedMonoBehaviourScript")).Click += delegate
            {
                if (withExviewerConfigCheck.Checked)
                {
                    connection.Mainform.OpenFolderDelegate(o =>
                    {
                        SaveByMonoBehaviourScript(
                            AssetList.GetSelectedAssets().Where(x => x.Type == ClassIDType.MonoBehaviour),
                            o.Folder,
                            withSkelCheck.Checked,
                            withPathIDCheck.Checked,
                            Convert.ToSingle(scaleTextBox.Text),
                            Convert.ToInt32(anisoLevelTextBox.Text),
                            Convert.ToSingle(posXTextBox.Text),
                            Convert.ToSingle(posYTextBox.Text),
                            edgePaddingCheck.Checked,
                            shaderCheck.Checked
                            );
                    });
                }
                else
                {
                    connection.Mainform.OpenFolderDelegate(o =>
                    {
                        SaveByMonoBehaviourScript(
                            AssetList.GetSelectedAssets().Where(x => x.Type == ClassIDType.MonoBehaviour),
                            o.Folder,
                            withSkelCheck.Checked,
                            withPathIDCheck.Checked
                            );
                    });
                }
            };
        }
    }
}