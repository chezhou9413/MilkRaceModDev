using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RimUnity.Editor;
using UnityEditor;
using UnityEngine;

//通过 RimUnity 标准导出器读取源 Shader 与材质，依赖由框架自动收集。
public static class ModRimUnityExport
{
    private const string ModRoot = "E:/RimModDev/MunoRace/MunoRace/MunoRace";

    //批处理完成后退出；失败保留完整报告，不生成自定义资源格式。
    public static void Export()
    {
        string oldTool = EditorPrefs.GetString("RimUnity.ShaderTool", "");
        string oldGame = EditorPrefs.GetString("RimUnity.GameRoot", "");
        var results = new List<BundleAssetResult>();
        try
        {
            EditorPrefs.SetString("RimUnity.ShaderTool", "E:/RimModDev/RimUnity/Tools/bin/ShaderTool/RimUnity.ShaderTool.exe");
            EditorPrefs.SetString("RimUnity.GameRoot", "E:/steam/steamapps/common/RimWorld");
            foreach (string path in Directory.GetFiles("Assets", "*.shader", SearchOption.AllDirectories).OrderBy(p => p))
                if (Path.GetFileNameWithoutExtension(path) != "LightningBloom")
                    Write(path.Replace('\\', '/'), "Shaders/" + Path.GetFileNameWithoutExtension(path));
            string report = ModRoot + "/Tools/Build/RimUnity-export-report.json";
            File.WriteAllText(report, JsonConvert.SerializeObject(results, Formatting.Indented), new System.Text.UTF8Encoding(false));
            if (results.Any(r => r.status != "exported")) throw new InvalidOperationException("资源导出失败，查看 " + report);
            Debug.Log("MOD_RIMUNITY_EXPORT_OK " + results.Count);
        }
        finally
        {
            EditorPrefs.SetString("RimUnity.ShaderTool", oldTool);
            EditorPrefs.SetString("RimUnity.GameRoot", oldGame);
        }

        //单项失败记录源路径，其他入口继续收集诊断。
        void Write(string source, string logical)
        {
            var result = new BundleAssetResult { path = source };
            try
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(source);
                if (asset == null) throw new FileNotFoundException("源资源不存在：" + source);
                PrefabExporter.Export(ModRoot, new ExportRequest { asset = asset, path = logical, description = source });
                result.type = asset.GetType().Name; result.status = "exported";
            }
            catch (Exception error) { result.status = "failed"; result.error = error.ToString(); Debug.LogError(source + "\n" + error); }
            results.Add(result);
        }
    }
}
