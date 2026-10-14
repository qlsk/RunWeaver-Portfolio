using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class RidiFontMigration
{
    const string Root = "Assets/Resources/Fonts/";
    static readonly Regex Reference = new Regex(@"\{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: \d+\}");
    static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>();
    static TMP_FontAsset font;
    static Font source;
    static int materialCount;

    [InitializeOnLoadMethod]
    static void Schedule()
    {
        if (!Application.isBatchMode && !File.Exists("output/ridi-font-migration.txt"))
            EditorApplication.delayCall += Run;
    }

    public static void Run()
    {
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode before font migration.");
            var scenes = Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
                .Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt).ToArray();
            if (scenes.Any(s => s.isDirty)) throw new Exception("Save open scene changes before font migration.");
            AssetDatabase.Refresh();
            source = AssetDatabase.LoadAssetAtPath<Font>(Root + "RIDIBatang.otf");
            if (!source) throw new Exception("RIDIBatang source import failed.");
            font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.name = "RIDIBatang SDF";
            AssetDatabase.CreateAsset(font, Root + "RIDIBatang SDF.asset");
            font.material.name = "RIDIBatang Atlas Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            font.atlasTextures[0].name = "RIDIBatang Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            const string sample = "룬 도감 잊혀진 유적 새 게임 계속하기 설정 종료 피해량 증가 감소 체력 마나 보상 선택 강화 원정 성공 실패";
            string missing;
            if (!font.TryAddCharacters(new string(Enumerable.Range(32, 95).Select(i => (char)i).ToArray()) + sample, out missing))
                throw new Exception("Missing test glyphs: " + missing);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();

            var paths = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Where(p => !p.StartsWith("Assets/19_ThirdParty/") && !p.StartsWith(Root)
                    && !p.StartsWith("Assets/10_UI/Fonts/") && !p.StartsWith("Assets/_Recovery/")
                    && new[] { ".unity", ".prefab", ".asset", ".mat", ".anim" }.Contains(Path.GetExtension(p))).ToList();
            paths.Add("Assets/19_ThirdParty/TextMesh Pro/Resources/TMP Settings.asset");
            int changed = 0;
            foreach (string path in paths)
            {
                string text = File.ReadAllText(path);
                if (!text.StartsWith("%YAML")) continue;
                string result = Reference.Replace(text, Replace);
                // Unity's built-in legacy font does not resolve through AssetDatabase.
                result = Regex.Replace(result, @"(m_Font: )\{fileID: 12800000, guid: 0000000000000000e000000000000000, type: 0\}", "$1" + Ref(source));
                if (text == result) continue;
                File.WriteAllText(path, result, new UTF8Encoding(false));
                changed++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            File.WriteAllText("output/ridi-font-migration.txt", "Changed files: " + changed + "\nMaterial presets: " + materialCount + "\nFont: " + Ref(font) + "\nSource: " + Ref(source) + "\n" + string.Join("\n", Replacements.Where(p => p.Key != p.Value).Select(p => p.Key + " => " + p.Value)));
            foreach (var scene in scenes)
                if (!string.IsNullOrEmpty(scene.path)) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path, UnityEditor.SceneManagement.OpenSceneMode.Single);
            Debug.Log("RIDI_MIGRATION_SUCCESS files=" + changed + " materials=" + materialCount);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            File.WriteAllText("output/ridi-font-error.txt", ex.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    static string Ref(UnityEngine.Object obj)
    {
        string guid;
        long id;
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out id)) throw new Exception("Unsaved asset: " + obj);
        return "{fileID: " + id + ", guid: " + guid + ", type: " + (obj is Font ? 3 : 2) + "}";
    }

    static string Replace(Match match)
    {
        string replacement;
        if (Replacements.TryGetValue(match.Value, out replacement)) return replacement;
        replacement = match.Value;
        string path = AssetDatabase.GUIDToAssetPath(match.Groups[2].Value);
        if (!string.IsNullOrEmpty(path) && !path.StartsWith(Root))
        {
            // Only fonts and materials can require conversion; avoid loading scenes and prefabs.
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".asset" || extension == ".mat" || extension == ".ttf" || extension == ".otf")
            {
                var obj = AssetDatabase.LoadAllAssetsAtPath(path).FirstOrDefault(o =>
                {
                    string guid;
                    long id;
                    return o && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out guid, out id) && id.ToString() == match.Groups[1].Value;
                });
                if (obj is TMP_FontAsset) replacement = Ref(font);
                else if (obj is Font) replacement = Ref(source);
                else if (obj is Material original && original.HasProperty("_GradientScale") && original.mainTexture
                    && AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GetAssetPath(original.mainTexture)))
                {
                    var material = new Material(original);
                    material.name = "RIDIBatang - " + original.name;
                    material.mainTexture = font.atlasTextures[0];
                    foreach (string property in new[] { "_TextureWidth", "_TextureHeight", "_GradientScale" })
                        material.SetFloat(property, font.material.GetFloat(property));
                    string target = Root + "RIDIBatang Preset " + (++materialCount).ToString("D2") + ".mat";
                    AssetDatabase.CreateAsset(material, target);
                    replacement = Ref(material);
                }
            }
        }
        Replacements[match.Value] = replacement;
        return replacement;
    }
}
