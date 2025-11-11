using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class DeepCallScanner
{
    public struct CallSite
    {
        public string assetPath; // "Assets/.../File.cs"
        public int lineNumber;   // 1-based
        public string lineText;  // trimmed
        public string methodName;
    }

    private static IEnumerable<string> EnumerateCsFiles()
    {
        var root = Application.dataPath;
        var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            // Normalize path separators to forward slashes for AssetDatabase compatibility
            var f = files[i].Replace('\\', '/');
            var assetPath = f.Replace(root, "Assets");
            yield return assetPath;
        }
    }

    private static IEnumerable<CallSite> ScanFileForPatterns(string assetPath, IList<(Regex pattern, string method)> patterns)
    {
        string[] lines = null;
        try { lines = File.ReadAllLines(assetPath.Replace("Assets", Application.dataPath)); }
        catch { yield break; }
        if (lines == null) yield break;

        for (int i = 0; i < lines.Length; i++)
        {
            var text = lines[i];
            if (string.IsNullOrEmpty(text)) continue;
            foreach (var p in patterns)
            {
                if (p.pattern.IsMatch(text))
                {
                    yield return new CallSite
                    {
                        assetPath = assetPath,
                        lineNumber = i + 1,
                        lineText = text.Trim(),
                        methodName = p.method
                    };
                    break;
                }
            }
        }
    }

    // --- Singleton-based scanning: TypeName.Instance.MethodName(...)
    public static List<CallSite> ScanSingletonCalls(string typeName, params string[] methodNames)
    {
        var results = new List<CallSite>();
        var patterns = new List<(Regex, string)>();
        foreach (var m in methodNames)
        {
            // Matches: InputManager.Instance.SetUiContext(
            // Allow null-conditional (?.) or null-forgiving (!.) between Instance and method
            // e.g., UIManager.Instance?.NotifyUiChange( ... ) or UIManager.Instance.NotifyUiChange(
            var sep = @"\s*(?:\?\s*\.\s*|!\s*\.\s*|\.\s*)";
            var r1 = new Regex($@"\b{Regex.Escape(typeName)}\s*\.\s*Instance{sep}{Regex.Escape(m)}\s*\(", RegexOptions.Compiled);
            // Matches namespaced: Managers.UIManager.Instance?.NotifyUiChange(
            var r2 = new Regex($@"\b(?:\w+\.)*{Regex.Escape(typeName)}\s*\.\s*Instance{sep}{Regex.Escape(m)}\s*\(", RegexOptions.Compiled);
            patterns.Add((r1, m));
            patterns.Add((r2, m));
        }

        foreach (var assetPath in EnumerateCsFiles())
        {
            results.AddRange(ScanFileForPatterns(assetPath, patterns));
        }
        return results;
    }

    // --- General method name usage scanning: looks for ".MethodName(" anywhere
    public static List<CallSite> ScanMethodNameUsage(params string[] methodNames)
    {
        var results = new List<CallSite>();
        var patterns = new List<(Regex, string)>();
        foreach (var m in methodNames)
        {
            var r = new Regex($@"\b{Regex.Escape(m)}\s*\(", RegexOptions.Compiled);
            patterns.Add((r, m));
        }
        foreach (var assetPath in EnumerateCsFiles())
        {
            results.AddRange(ScanFileForPatterns(assetPath, patterns));
        }
        return results;
    }

    public static void OpenAt(string assetPath, int line)
    {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
        if (script != null)
        {
            AssetDatabase.OpenAsset(script, line);
        }
        else
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (obj != null)
                Selection.activeObject = obj;
        }
    }
}