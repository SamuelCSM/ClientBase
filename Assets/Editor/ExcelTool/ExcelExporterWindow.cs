using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.ExcelTool
{
    /// <summary>
    /// Excel 导出器窗口
    /// 用于将 Excel 数据导出到 SQLite 数据库
    /// </summary>
    public class ExcelExporterWindow : EditorWindow
    {
        private enum ExportMode
        {
            Single,   // 单个文件导出
            Batch     // 批量导出
        }

        private ExportMode _mode = ExportMode.Single;
        private string _excelPath = "";
        private string _excelFolder = "";
        private string _outputDbPath = "Assets/StreamingAssets/Config.db";
        private bool _overwriteExistingTables = true;
        private bool _enableValidation = true;
        private bool _verboseLogging = false;
        private Vector2 _scrollPosition;
        private List<ExcelExporter.ExportResult> _lastResults = new List<ExcelExporter.ExportResult>();

        [MenuItem("Tools/Excel/Excel 导出器")]
        public static void ShowWindow()
        {
            var window = GetWindow<ExcelExporterWindow>("Excel 导出器");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // 模式选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("导出模式:", GUILayout.Width(80));
            _mode = (ExportMode)EditorGUILayout.EnumPopup(_mode, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 根据模式显示不同的输入
            if (_mode == ExportMode.Single)
            {
                DrawSingleModeUI();
            }
            else
            {
                DrawBatchModeUI();
            }

            EditorGUILayout.Space(10);

            // 输出数据库路径
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("输出数据库:", GUILayout.Width(80));
            _outputDbPath = EditorGUILayout.TextField(_outputDbPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.SaveFilePanel("选择输出数据库", Application.dataPath, "Config", "db");
                if (!string.IsNullOrEmpty(path))
                {
                    _outputDbPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 导出选项
            EditorGUILayout.LabelField("导出选项:", EditorStyles.boldLabel);
            _overwriteExistingTables = EditorGUILayout.Toggle("覆盖已存在的表", _overwriteExistingTables);
            _enableValidation = EditorGUILayout.Toggle("启用数据校验", _enableValidation);
            _verboseLogging = EditorGUILayout.Toggle("显示详细日志", _verboseLogging);

            EditorGUILayout.Space(10);

            // 导出按钮
            if (GUILayout.Button("开始导出", GUILayout.Height(35)))
            {
                Export();
            }

            EditorGUILayout.Space(10);

            // 显示导出结果
            if (_lastResults.Count > 0)
            {
                DrawResults();
            }
        }

        /// <summary>
        /// 绘制单个文件模式 UI
        /// </summary>
        private void DrawSingleModeUI()
        {
            EditorGUILayout.HelpBox("导出单个 Excel 文件到 SQLite 数据库", MessageType.Info);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Excel 文件:", GUILayout.Width(80));
            _excelPath = EditorGUILayout.TextField(_excelPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFilePanel("选择 Excel 文件", Application.dataPath, "xlsx");
                if (!string.IsNullOrEmpty(path))
                {
                    _excelPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制批量模式 UI
        /// </summary>
        private void DrawBatchModeUI()
        {
            EditorGUILayout.HelpBox("批量导出文件夹中的所有 Excel 文件", MessageType.Info);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Excel 文件夹:", GUILayout.Width(80));
            _excelFolder = EditorGUILayout.TextField(_excelFolder);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("选择 Excel 文件夹", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _excelFolder = path;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 导出
        /// </summary>
        private void Export()
        {
            _lastResults.Clear();

            try
            {
                // 创建导出配置
                var config = new ExcelExporter.ExportConfig
                {
                    OutputDbPath = _outputDbPath,
                    OverwriteExistingTables = _overwriteExistingTables,
                    EnableValidation = _enableValidation,
                    VerboseLogging = _verboseLogging
                };

                var exporter = new ExcelExporter(config);

                if (_mode == ExportMode.Single)
                {
                    // 单个文件导出
                    if (string.IsNullOrEmpty(_excelPath) || !File.Exists(_excelPath))
                    {
                        EditorUtility.DisplayDialog("错误", "请选择有效的 Excel 文件", "确定");
                        return;
                    }

                    var result = exporter.ExportExcel(_excelPath);
                    _lastResults.Add(result);

                    if (result.Success)
                    {
                        EditorUtility.DisplayDialog("成功", 
                            $"导出成功!\n表名: {result.TableName}\n行数: {result.RowCount}", 
                            "确定");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("失败", 
                            $"导出失败:\n{result.ErrorMessage}", 
                            "确定");
                    }
                }
                else
                {
                    // 批量导出
                    if (string.IsNullOrEmpty(_excelFolder) || !Directory.Exists(_excelFolder))
                    {
                        EditorUtility.DisplayDialog("错误", "请选择有效的 Excel 文件夹", "确定");
                        return;
                    }

                    // 查找所有 Excel 文件
                    var excelFiles = Directory.GetFiles(_excelFolder, "*.xlsx", SearchOption.AllDirectories)
                        .Where(f => !Path.GetFileName(f).StartsWith("~$")) // 排除临时文件
                        .ToList();

                    if (excelFiles.Count == 0)
                    {
                        EditorUtility.DisplayDialog("错误", "文件夹中没有找到 Excel 文件", "确定");
                        return;
                    }

                    // 批量导出
                    _lastResults = exporter.ExportBatch(excelFiles);

                    // 显示结果
                    var successCount = _lastResults.Count(r => r.Success);
                    var failCount = _lastResults.Count(r => !r.Success);

                    EditorUtility.DisplayDialog("完成", 
                        $"批量导出完成!\n成功: {successCount}\n失败: {failCount}", 
                        "确定");
                }

                // 刷新资源
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"导出过程中发生错误:\n{ex.Message}", "确定");
                Debug.LogError($"[ExcelExporterWindow] {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 绘制导出结果
        /// </summary>
        private void DrawResults()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("导出结果:", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));

            foreach (var result in _lastResults)
            {
                EditorGUILayout.BeginVertical("box");

                // 表名和状态
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("表名:", GUILayout.Width(60));
                EditorGUILayout.LabelField(result.TableName ?? "未知", EditorStyles.boldLabel);
                
                var statusColor = result.Success ? Color.green : Color.red;
                var oldColor = GUI.color;
                GUI.color = statusColor;
                EditorGUILayout.LabelField(result.Success ? "✓ 成功" : "✗ 失败", GUILayout.Width(60));
                GUI.color = oldColor;
                
                EditorGUILayout.EndHorizontal();

                // 行数
                if (result.Success)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("行数:", GUILayout.Width(60));
                    EditorGUILayout.LabelField(result.RowCount.ToString());
                    EditorGUILayout.EndHorizontal();
                }

                // 错误消息
                if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                {
                    EditorGUILayout.HelpBox(result.ErrorMessage, MessageType.Error);
                }

                // 警告消息
                if (result.Warnings.Count > 0)
                {
                    foreach (var warning in result.Warnings)
                    {
                        EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
