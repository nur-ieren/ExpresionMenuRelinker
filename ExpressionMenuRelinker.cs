#if UNITY_EDITOR
// 配置場所: Assets/nur_ieren/ExpressionMenuRelinker/ExpressionMenuRelinker.cs
// Editorフォルダ・asmdefに依存せず、#if UNITY_EDITOR のみで動作します。
// VRC SDKの型にも依存しないため、SDKのバージョン差異で壊れません。
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace nur_ieren.ExpressionMenuRelinker
{
    public class ExpressionMenuRelinkerWindow : EditorWindow
    {
        private class Entry
        {
            public string menuPath;     // 書き換え対象のMenuアセット
            public int controlIndex;    // controls配列内の位置
            public string controlName;
            public string oldPath;
            public string newPath;
            public bool selected = true;
        }

        private DefaultAsset sourceFolder;
        private DefaultAsset destFolder;
        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<string> unresolved = new List<string>();
        private Vector2 scroll;
        private bool scanned;
        private int destMenuCount;

        [MenuItem("nur_ieren/ExpressionMenu 参照付け替え")]
        private static void Open()
        {
            var w = GetWindow<ExpressionMenuRelinkerWindow>("ExpressionMenu 参照付け替え");
            w.minSize = new Vector2(480, 360);
            w.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "コピー先フォルダ内のExpressionMenuが、コピー元フォルダ内のMenuを参照している場合に、" +
                "コピー先フォルダ内の同じ相対パス(なければ同名)のMenuへ付け替えます。\n" +
                "コピー元を空欄にすると、「コピー先フォルダ外を参照していて、コピー先内に同名Menuが1つだけある」参照を対象にします。",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField("コピー元フォルダ (任意)", sourceFolder, typeof(DefaultAsset), false);
            destFolder = (DefaultAsset)EditorGUILayout.ObjectField("コピー先フォルダ", destFolder, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                scanned = false;
                entries.Clear();
                unresolved.Clear();
            }

            string srcPath = GetFolderPath(sourceFolder);
            string dstPath = GetFolderPath(destFolder);

            if (sourceFolder != null && srcPath == null)
                EditorGUILayout.HelpBox("コピー元にはフォルダを指定してください。", MessageType.Warning);
            if (destFolder != null && dstPath == null)
                EditorGUILayout.HelpBox("コピー先にはフォルダを指定してください。", MessageType.Warning);
            if (srcPath != null && dstPath != null && srcPath == dstPath)
                EditorGUILayout.HelpBox("コピー元とコピー先が同じです。", MessageType.Warning);

            bool canScan = dstPath != null && (sourceFolder == null || srcPath != null) && srcPath != dstPath;

            using (new EditorGUI.DisabledScope(!canScan))
            {
                if (GUILayout.Button("スキャン", GUILayout.Height(26)))
                    Scan(srcPath, dstPath);
            }

            if (!scanned) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"コピー先のMenu数: {destMenuCount} / 付け替え対象: {entries.Count} / 未解決: {unresolved.Count}");

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var e in entries)
            {
                EditorGUILayout.BeginVertical("box");
                e.selected = EditorGUILayout.ToggleLeft(
                    $"{System.IO.Path.GetFileName(e.menuPath)}  >  [{e.controlIndex}] {e.controlName}", e.selected);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("現在", e.oldPath, EditorStyles.miniLabel);
                EditorGUILayout.LabelField("変更後", e.newPath, EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            if (unresolved.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("未解決 (コピー先に対応するMenuが見つからない)", EditorStyles.boldLabel);
                foreach (var u in unresolved)
                    EditorGUILayout.LabelField(u, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(!entries.Any(x => x.selected)))
            {
                if (GUILayout.Button("付け替え実行", GUILayout.Height(30)))
                    Apply();
            }
        }

        private static string GetFolderPath(Object obj)
        {
            if (obj == null) return null;
            string p = AssetDatabase.GetAssetPath(obj);
            return AssetDatabase.IsValidFolder(p) ? p : null;
        }

        private static bool IsUnder(string path, string folder)
        {
            return path.StartsWith(folder + "/", System.StringComparison.Ordinal);
        }

        private void Scan(string srcPath, string dstPath)
        {
            entries.Clear();
            unresolved.Clear();

            var destMenus = AssetDatabase.FindAssets("t:VRCExpressionsMenu", new[] { dstPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .ToList();
            destMenuCount = destMenus.Count;

            var byRelative = new Dictionary<string, string>();
            var byName = new Dictionary<string, List<string>>();
            foreach (var p in destMenus)
            {
                byRelative[p.Substring(dstPath.Length + 1)] = p;
                string n = System.IO.Path.GetFileName(p);
                if (!byName.TryGetValue(n, out var list))
                    byName[n] = list = new List<string>();
                list.Add(p);
            }

            foreach (var menuPath in destMenus)
            {
                var menu = AssetDatabase.LoadMainAssetAtPath(menuPath);
                if (menu == null) continue;

                var so = new SerializedObject(menu);
                var controls = so.FindProperty("controls");
                if (controls == null || !controls.isArray) continue;

                for (int i = 0; i < controls.arraySize; i++)
                {
                    var ctrl = controls.GetArrayElementAtIndex(i);
                    var sub = ctrl.FindPropertyRelative("subMenu");
                    if (sub == null || sub.objectReferenceValue == null) continue;

                    string oldPath = AssetDatabase.GetAssetPath(sub.objectReferenceValue);
                    if (string.IsNullOrEmpty(oldPath) || IsUnder(oldPath, dstPath)) continue;

                    string newPath = null;
                    if (srcPath != null)
                    {
                        if (!IsUnder(oldPath, srcPath)) continue;
                        string rel = oldPath.Substring(srcPath.Length + 1);
                        if (!byRelative.TryGetValue(rel, out newPath))
                            newPath = FindUniqueByName(byName, oldPath);
                    }
                    else
                    {
                        newPath = FindUniqueByName(byName, oldPath);
                    }

                    string ctrlName = ctrl.FindPropertyRelative("name")?.stringValue ?? "";
                    if (newPath == null)
                    {
                        if (srcPath != null)
                            unresolved.Add($"{System.IO.Path.GetFileName(menuPath)} > [{i}] {ctrlName}  →  {oldPath}");
                        continue;
                    }

                    entries.Add(new Entry
                    {
                        menuPath = menuPath,
                        controlIndex = i,
                        controlName = ctrlName,
                        oldPath = oldPath,
                        newPath = newPath
                    });
                }
            }

            scanned = true;
        }

        private static string FindUniqueByName(Dictionary<string, List<string>> byName, string oldPath)
        {
            string n = System.IO.Path.GetFileName(oldPath);
            return byName.TryGetValue(n, out var list) && list.Count == 1 ? list[0] : null;
        }

        private void Apply()
        {
            int done = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var group in entries.Where(e => e.selected).GroupBy(e => e.menuPath))
                {
                    var menu = AssetDatabase.LoadMainAssetAtPath(group.Key);
                    if (menu == null) continue;

                    Undo.RecordObject(menu, "ExpressionMenu 参照付け替え");
                    var so = new SerializedObject(menu);
                    var controls = so.FindProperty("controls");

                    foreach (var e in group)
                    {
                        if (e.controlIndex >= controls.arraySize) continue;
                        var sub = controls.GetArrayElementAtIndex(e.controlIndex).FindPropertyRelative("subMenu");
                        var target = AssetDatabase.LoadMainAssetAtPath(e.newPath);
                        if (sub == null || target == null) continue;
                        sub.objectReferenceValue = target;
                        done++;
                    }

                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(menu);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[nur_ieren] ExpressionMenu参照付け替え: {done} 件を付け替えました。");

            Scan(GetFolderPath(sourceFolder), GetFolderPath(destFolder));
        }
    }
}
#endif
