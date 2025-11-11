using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ComponentLocatorWindow : EditorWindow
{
    private string _typeQuery = "StartMenuControlAnim"; // valor inicial útil
    private List<Occurrence> _results;
    private Vector2 _scroll;

    private class Occurrence
    {
        public string assetPath;      // Ex.: "Assets/Scenes/_Main.unity" ou "Assets/Prefabs/Some.prefab"
        public string contextType;    // "Scene" ou "Prefab"
        public string objectPath;     // Hierarquia: Root/Child/Sub
        public string componentType;  // Nome completo do tipo
    }

    [MenuItem("Tools/Localizador de Componentes")] 
    public static void ShowWindow()
    {
        var w = GetWindow<ComponentLocatorWindow>(true, "Localizador de Componentes");
        w.minSize = new Vector2(700, 380);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Localiza ocorrências de um componente em Prefabs e Cenas", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _typeQuery = EditorGUILayout.TextField("Componente (tipo)", _typeQuery);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Varrer Prefabs", GUILayout.Height(24)))
            {
                _results = ScanPrefabs(_typeQuery);
            }
            if (GUILayout.Button("Varrer Cenas", GUILayout.Height(24)))
            {
                _results = ScanScenes(_typeQuery);
            }
            if (GUILayout.Button("Varrer Tudo", GUILayout.Height(24)))
            {
                var a = ScanPrefabs(_typeQuery);
                var b = ScanScenes(_typeQuery);
                a.AddRange(b);
                _results = a;
            }
            if (GUILayout.Button("Limpar", GUILayout.Width(90)))
            {
                _results = null;
            }
        }

        EditorGUILayout.Space(6);

        if (_results == null)
        {
            EditorGUILayout.HelpBox("Informe o tipo de componente e clique em varrer.", MessageType.Info);
            return;
        }

        if (_results.Count == 0)
        {
            EditorGUILayout.HelpBox("Nenhuma ocorrência encontrada.", MessageType.Warning);
            return;
        }

        // Resumo
        var total = _results.Count;
        var porAsset = _results
            .GroupBy(r => r.assetPath)
            .Select(g => (asset: g.Key, count: g.Count()))
            .OrderBy(x => x.asset)
            .ToList();
        EditorGUILayout.LabelField($"Resultados: {total} ocorrência(s) em {porAsset.Count} asset(s)");

        using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scroll.scrollPosition;
            foreach (var grp in porAsset)
            {
                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{grp.asset} — {grp.count}", EditorStyles.boldLabel);
                    if (GUILayout.Button("Abrir asset", GUILayout.Width(100)))
                    {
                        OpenAsset(grp.asset);
                    }
                }

                foreach (var r in _results.Where(x => x.assetPath == grp.asset))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{r.contextType}: {r.objectPath} [{r.componentType}]", GUILayout.MinWidth(400));
                        if (GUILayout.Button("Abrir aqui", GUILayout.Width(100)))
                        {
                            OpenAt(r);
                        }
                    }
                }
            }
        }
    }

    private static List<Occurrence> ScanPrefabs(string typeQuery)
    {
        var list = new List<Occurrence>();
        var guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) continue;
            var comps = root.GetComponentsInChildren<Component>(true);
            foreach (var c in comps)
            {
                if (TypeMatches(c, typeQuery))
                {
                    var objPath = BuildHierarchyPath(c.gameObject.transform);
                    list.Add(new Occurrence
                    {
                        assetPath = path,
                        contextType = "Prefab",
                        objectPath = objPath,
                        componentType = c.GetType().FullName
                    });
                }
            }
        }
        return list;
    }

    private static List<Occurrence> ScanScenes(string typeQuery)
    {
        var list = new List<Occurrence>();
        var guids = AssetDatabase.FindAssets("t:Scene");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            UnityEngine.SceneManagement.Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
            catch
            {
                continue;
            }

            try
            {
                var roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    var comps = roots[i].GetComponentsInChildren<Component>(true);
                    foreach (var c in comps)
                    {
                        if (TypeMatches(c, typeQuery))
                        {
                            var objPath = BuildHierarchyPath(c.gameObject.transform);
                            list.Add(new Occurrence
                            {
                                assetPath = path,
                                contextType = "Scene",
                                objectPath = objPath,
                                componentType = c.GetType().FullName
                            });
                        }
                    }
                }
            }
            finally
            {
                // Fecha a cena sem salvar alterações
                try { EditorSceneManager.CloseScene(scene, true); } catch { /* ignore */ }
            }
        }
        return list;
    }

    private static bool TypeMatches(Component c, string query)
    {
        if (c == null || string.IsNullOrWhiteSpace(query)) return false;
        var t = c.GetType();
        var q = query.Trim();
        return string.Equals(t.Name, q, StringComparison.OrdinalIgnoreCase)
               || (!string.IsNullOrEmpty(t.FullName) && t.FullName.EndsWith(q, StringComparison.OrdinalIgnoreCase))
               || string.Equals(t.FullName, q, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildHierarchyPath(Transform t)
    {
        var names = new List<string>();
        while (t != null)
        {
            names.Add(t.name);
            t = t.parent;
        }
        names.Reverse();
        return string.Join("/", names);
    }

    private static void OpenAsset(string assetPath)
    {
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (obj != null)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
            AssetDatabase.OpenAsset(obj);
        }
    }

    private static void OpenAt(Occurrence occ)
    {
        if (occ == null) return;
        if (occ.contextType == "Prefab")
        {
            try
            {
                // Tenta abrir em Prefab Mode e selecionar o objeto
                var stage = UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(occ.assetPath);
                var go = GameObject.Find(occ.objectPath);
                if (go != null)
                {
                    Selection.activeObject = go;
                    EditorGUIUtility.PingObject(go);
                }
                else
                {
                    OpenAsset(occ.assetPath);
                }
            }
            catch
            {
                OpenAsset(occ.assetPath);
            }
        }
        else // Scene
        {
            try
            {
                EditorSceneManager.OpenScene(occ.assetPath, OpenSceneMode.Single);
                var go = GameObject.Find(occ.objectPath);
                if (go != null)
                {
                    Selection.activeObject = go;
                    EditorGUIUtility.PingObject(go);
                }
                else
                {
                    // Abre a cena e deixa usuário navegar
                    var sceneObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(occ.assetPath);
                    if (sceneObj != null)
                    {
                        Selection.activeObject = sceneObj;
                        EditorGUIUtility.PingObject(sceneObj);
                    }
                }
            }
            catch
            {
                OpenAsset(occ.assetPath);
            }
        }
    }
}