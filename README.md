# PluginsVer
在[`RazTools/Studio`](https://github.com/RazTools/Studio)基础上增加Plugins扩展，可以在Plugin菜单中使用这些指令。

* ## Live2D
	1. ExtractFromFolder：[`UnityCNLive2DExtractor`](https://github.com/Razmoth/UnityCNLive2DExtractor)的Gui版本，用于可用`Container`完成分组的常规L2D资源。
	* ### 使用：
		1. `Options`中选择可用的正确解包方式。
		2. `ExtractFromFolder`菜单选中解包资源所在文件夹。
		3. 选择要导出到的文件夹位置。

		**注意`MonoBehaviour`资源可能的dll程序集载入需求，一些情况下需要使用[`Il2CppDumper`](https://github.com/Perfare/Il2CppDumper)或其他工具dump出用于解包的DummyDll。**

	2. 每组L2D资源(除`Motion`以外)对应唯一的`GameObject`，通过涵盖这些资源信息的`GameObject`可以反向映射获取一组L2D资源(`Studio`默认状态不会显示`GameObject`，这里通过`Animator`间接获取对应的`GameObject`)。

	* ### 使用前的配置：
		`ExportWithPathIDFolder`：导出时额外增加一级以`PathID`命名的目录(排除预期之外的覆盖行为)。

	* ### 使用：
		* `SaveByAllAnimators`：从所有`Animator`中导出可用的L2D资源组。
		* `SaveBySelectedAnimators`：从选中的所有`Animator`中导出可用的L2D资源组。

		**注意`MonoBehaviour`资源可能的dll程序集载入需求，一些情况下需要使用[`Il2CppDumper`](https://github.com/Perfare/Il2CppDumper)或其他工具dump出用于解包的DummyDll。**

	* ### 缺陷：
		**由于`Gameobject`与`Motions`资源并无直接关联，使用此方案需要配合`SaveMotionsBySelected`。**

	* ### 其它:
		* `SaveMotionsBySelected`：从选中的所有`Animator`以及`AnimationClip`中导出可能的`.motion3.json`文件。
		* `ForceSaveAsMoc3`：从选中的所有`MonoBehaviour`中导出可用的`.moc3`文件。
		* `ForceSaveAsPhysics`：从选中的所有`MonoBehaviour`中导出可用的`.physics3.json`文件。

* ## Spine
	每组Spine资源对应唯一的`MonoBehaviour`，通过涵盖这些Spine资源信息的`MonoBehaviour`可以反向映射获取一组Spine资源。
	
	* ### 使用前的配置：
		* `ExportExviewerConfig`：生成用于[`Live2DViewerEX`](https://store.steampowered.com/app/616720/Live2DViewerEX/)的配置文件。
		* `ExportWithSkelFolder`：导出时额外增加一级以`Spine骨架文件名`命名的目录。
		* `ExportWithPathIDFolder`：导出时额外增加一级以`PathID`命名的目录(排除预期之外的覆盖行为)。

	* ### 使用：
		* `SaveByAllMonoBehaviourScript`：从所有的`MonoBehaviour`中导出可用的Spine资源组。
		* `SaveBySelectedMonoBehaviourScript`：从选中的`MonoBehaviour`中导出可用的Spine资源组。

		**注意`MonoBehaviour`资源可能的dll程序集载入需求，一些情况下需要使用[`Il2CppDumper`](https://github.com/Perfare/Il2CppDumper)或其他工具dump出用于解包的DummyDll。**
  
******

# Studio
Check out the [original AssetStudio project](https://github.com/Perfare/AssetStudio) for more information.

Note: Requires Internet connection to fetch asset_index jsons.
_____________________________________________________________________________________________________________________________
How to use:

Check the tutorial [here](https://gist.github.com/Modder4869/0f5371f8879607eb95b8e63badca227e) (Thanks to Modder4869 for the tutorial)
_____________________________________________________________________________________________________________________________
CLI Version:
```
Description:

Usage:
  AssetStudioCLI <input_path> <output_path> [options]

Arguments:
  <input_path>   Input file/folder.
  <output_path>  Output folder.

Options:
  --silent                                                Hide log messages.
  --type <Texture2D|Sprite|etc..>                         Specify unity class type(s)
  --filter <filter>                                       Specify regex filter(s).
  --game <BH3|CB1|CB2|CB3|GI|SR|TOT|ZZZ> (REQUIRED)       Specify Game.
  --map_op <AssetMap|Both|CABMap|None>                    Specify which map to build. [default: None]
  --map_type <JSON|XML>                                   AssetMap output type. [default: XML]
  --map_name <map_name>                                   Specify AssetMap file name.
  --group_assets_type <ByContainer|BySource|ByType|None>  Specify how exported assets should be grouped. [default: 0]
  --no_asset_bundle                                       Exclude AssetBundle from AssetMap/Export.
  --no_index_object                                       Exclude IndexObject/MiHoYoBinData from AssetMap/Export.
  --xor_key <xor_key>                                     XOR key to decrypt MiHoYoBinData.
  --ai_file <ai_file>                                     Specify asset_index json file path (to recover GI containers).
  --version                                               Show version information
  -?, -h, --help                                          Show help and usage information
```
_____________________________________________________________________________________________________________________________
NOTES:
```
- in case of any "MeshRenderer/SkinnedMeshRenderer" errors, make sure to enable "Disable Renderer" option in "Export Options" before loading assets.
- in case of need to export models/animators without fetching all animations, make sure to enable "Ignore Controller Anim" option in "Options -> Export Options" before loading assets.
```
_____________________________________________________________________________________________________________________________
Special Thank to:
- Perfare: Original author.
- Khang06: [Project](https://github.com/khang06/genshinblkstuff) for extraction.
- Radioegor146: [Asset-indexes](https://github.com/radioegor146/gi-asset-indexes) for recovered/updated asset_index's.
- Ds5678: [AssetRipper](https://github.com/AssetRipper/AssetRipper)[[discord](https://discord.gg/XqXa53W2Yh)] for information about Asset Formats & Parsing.
- mafaca: [uTinyRipper](https://github.com/mafaca/UtinyRipper) for `YAML` and `AnimationClipConverter`. 
