using System;
using System.IO;
using System.Text.Json;
using ChezhouLib.ALLmap;
using ChezhouLib.LibDef;
using RimUnity.Assets;

namespace MunoRaceLib.Core.Assets;

//在 CL 创建材质模板前加载 RimUnity 标准清单，保留现有业务资源键。
public sealed class MunoNativeResourceProvider : ClNativeResourceProvider
{
    internal static Verse.ModContentPack Content { get; private set; }
    private static bool initialized;
    private static Exception failure;

    //初始化失败明确报告原因，不回退旧 AssetBundle。
    public override void InitializeResources()
    {
        if (initialized) return;
        if (failure != null) throw new InvalidOperationException("MunoRace RimUnity 资源初始化失败。", failure);
        try
        {
            Content = modContentPack;
            string package = modContentPack.PackageId.ToLowerInvariant();
            using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(modContentPack.RootDir, "1.7/Data/RimUnity/catalog.json")));
            foreach (var entry in catalog.RootElement.GetProperty("assets").EnumerateArray())
            {
                string path = entry.GetProperty("path").GetString();
                if (path.StartsWith("Shaders/", StringComparison.Ordinal))
                {
                    var exported = AssetLoader.Load<RimUnity.Shader>(modContentPack, path);
                    var shader = HostAssets.ToHost(exported);
                    foreach (string alias in new[] { exported.name, Path.GetFileName(exported.name) })
                    {
                        abDatabase.shaderDataBase[package + "=>" + alias] = shader;
                        abDatabase.shaderDataBase[package + "=>" + alias.ToLowerInvariant()] = shader;
                    }
                }
                else if (path.StartsWith("Textures/", StringComparison.Ordinal))
                    abDatabase.Texture2dDataBase.Add(package + "=>" + Path.GetFileName(path),
                        HostAssets.ToHost(AssetLoader.Load<RimUnity.Texture2D>(modContentPack, path)));
            }
            initialized = true;
        }
        catch (Exception error) { failure = error; throw; }
    }
}
