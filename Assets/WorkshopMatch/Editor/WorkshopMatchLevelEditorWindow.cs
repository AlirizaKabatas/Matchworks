using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace WorkshopMatch.Editor
{
    public sealed class WorkshopMatchLevelEditorWindow : EditorWindow
    {
        private enum Brush
        {
            Candy,
            Bolt,
            Motor,
            FrozenGrease,
            Oil,
            Erase,
            RayHorizontal,
            RayVertical,
            Hammer,
            Magnet
        }

        private const string LevelDirectory = "Assets/WorkshopMatch/Resources/WorkshopMatch/Levels";
        private LevelDefinition level;
        private List<LevelValidationMessage> validation = new();
        private Vector2 scroll;
        private Brush brush = Brush.Bolt;
        private int candyColor = -1;
        private bool motorBrushRotated;

        [MenuItem("Window/Workshop Match/Level Editor")]
        public static void Open()
        {
            var window=GetWindow<WorkshopMatchLevelEditorWindow>("Workshop Match Level");
            window.Load(LevelRepository.SelectedLevelId,false);
        }

        private void OnEnable()
        {
            minSize = new Vector2(640f, 620f);
            if (level == null)
                Load(LevelRepository.SelectedLevelId, false);
        }

        private void OnGUI()
        {
            if (level == null)
            {
                EditorGUILayout.HelpBox("Bölüm verisi yüklenemedi.", MessageType.Error);
                if (GUILayout.Button("Geçici Taslak Oluştur")) level = LevelRepository.CreatePrototype();
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            EditorGUILayout.Space(8f);
            DrawBrushes();
            EditorGUILayout.Space(8f);
            DrawGrid();
            EditorGUILayout.Space(8f);
            DrawSummary();
            DrawValidation();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Bölüm", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                level.Id = EditorGUILayout.TextField("Kimlik", level.Id);
                if (GUILayout.Button("Yükle", GUILayout.Width(80f)))
                Load(level.Id, true);
                if (GUILayout.Button("Kaydet", GUILayout.Width(80f))) Save();
                if (GUILayout.Button("Bu Bölümü Oyna", GUILayout.Width(145f))) PlayThisLevel();
            }
            level.DisplayTitle = EditorGUILayout.TextField("Başlık", level.DisplayTitle);
            level.IsDraft = EditorGUILayout.Toggle("Taslak", level.IsDraft);
            using (new EditorGUILayout.HorizontalScope())
            {
                int width = EditorGUILayout.IntField("Genişlik", level.Width);
                int height = EditorGUILayout.IntField("Yükseklik", level.Height);
                level.Width = Mathf.Clamp(width, 3, 12);
                level.Height = Mathf.Clamp(height, 3, 14);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                level.Moves = Mathf.Max(1, EditorGUILayout.IntField("Hamle", level.Moves));
                level.ColorCount = Mathf.Clamp(EditorGUILayout.IntField("Renk", level.ColorCount), 3, 5);
                level.SpawnSeed = EditorGUILayout.IntField("Seed", level.SpawnSeed);
            }
            level.ChapterMachineId = EditorGUILayout.TextField("Makine Kimliği", level.ChapterMachineId);
            motorBrushRotated = EditorGUILayout.Toggle("Yeni motor dikey (2×3)", motorBrushRotated);
        }

        private void DrawBrushes()
        {
            EditorGUILayout.LabelField("Fırça", EditorStyles.boldLabel);
            brush = (Brush)GUILayout.Toolbar((int)brush,
                new[] { "Şeker", "Civata", "Motor", "Donmuş Yağ", "Zemin Yağı", "Sil", "→ Fişek", "↑ Fişek", "Çekiç", "Mıknatıs" });
            if (brush == Brush.Candy)
                candyColor = EditorGUILayout.IntSlider("Sabit renk (-1 rastgele)", candyColor, -1, level.ColorCount - 1);
            EditorGUILayout.HelpBox("Motor seçilen kareyi alt-sol ankraj kabul eder. Yeni motor dikey seçimi yalnız yerleştirdiğin motoru etkiler: yatay 3×2, dikey 2×3. Bir şeyi taşımak için eski kareyi Sil, sonra yeni kareye tıkla.", MessageType.None);
        }

        private void DrawGrid()
        {
            EditorGUILayout.LabelField($"Tahta {level.Width}×{level.Height} — üst sıra y={level.Height - 1}", EditorStyles.boldLabel);
            float buttonSize = Mathf.Clamp((position.width - 80f) / Mathf.Max(1, level.Width), 34f, 58f);
            for (int y = level.Height - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(y.ToString(), GUILayout.Width(22f), GUILayout.Height(buttonSize));
                    for (int x = 0; x < level.Width; x++)
                    {
                        LevelCellData cell = FindCell(x, y);
                        LevelCellData displayCell = FindMotorAnchorCovering(x,y)?.Copy() ?? cell;
                        if(displayCell!=null&&displayCell.Content==CellContent.Motor)displayCell.Oil=cell!=null&&cell.Oil;
                        GUI.backgroundColor = CellColor(displayCell);
                        if (GUILayout.Button(CellLabel(displayCell), GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                        {
                            ApplyBrush(x, y);
                            Validate();
                            GUI.FocusControl(null);
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(24f);
                for (int x = 0; x < level.Width; x++)
                    GUILayout.Label(x.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(buttonSize));
            }
        }

        private void DrawSummary()
        {
            int bolts = 0;
            int oils = 0;
            int motors = 0;
            int frozen = 0;
            foreach (LevelCellData cell in level.Cells ?? new List<LevelCellData>())
            {
                if (cell == null) continue;
                if (cell.Content == CellContent.Bolt) bolts++;
                if (cell.Content == CellContent.Motor) motors++;
                if (cell.Content == CellContent.FrozenGrease) frozen++;
                if (cell.Oil) oils++;
            }
            EditorGUILayout.LabelField("Hedef özeti", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Civata: {bolts}   Zemin yağı: {oils}   Motor: {motors}   Donmuş yağ: {frozen}   Hamle: {level.Moves}");
            if (GUILayout.Button("Şimdi Doğrula", GUILayout.Width(120f))) Validate();
        }

        private void DrawValidation()
        {
            if (validation.Count == 0) Validate();
            foreach (LevelValidationMessage message in validation)
            {
                MessageType type = message.Severity switch
                {
                    LevelValidationSeverity.Error => MessageType.Error,
                    LevelValidationSeverity.Warning => MessageType.Warning,
                    _ => MessageType.Info
                };
                EditorGUILayout.HelpBox(message.Text, type);
            }
        }

        private void ApplyBrush(int x, int y)
        {
            level.Cells ??= new List<LevelCellData>();
            LevelCellData cell = FindCell(x, y);
            LevelCellData coveringMotor = FindMotorAnchorCovering(x, y);
            if (brush == Brush.Erase)
            {
                if (coveringMotor != null) level.Cells.Remove(coveringMotor);
                level.Cells.RemoveAll(item => item != null && item.X == x && item.Y == y);
                return;
            }

            if (coveringMotor != null && coveringMotor != cell && brush != Brush.Oil)
            {
                ShowNotification(new GUIContent("Bu kare motor alanında; önce motoru sil."));
                return;
            }

            cell ??= AddCell(x, y);
            switch (brush)
            {
                case Brush.Candy:
                    cell.Content = CellContent.Candy;
                    cell.ObstacleState = 0;
                    cell.CandyColor = candyColor;
                    cell.Special=SpecialKind.None;
                    break;
                case Brush.Bolt:
                    cell.Content = CellContent.Bolt;
                    cell.ObstacleState = 2;
                    cell.CandyColor = -1;
                    cell.Special=SpecialKind.None;
                    break;
                case Brush.Motor:
                    cell = FindCell(x, y) ?? AddCell(x, y);
                    cell.Content = CellContent.Motor;
                    cell.MotorOrientation=motorBrushRotated?2:1;
                    cell.ObstacleState = 5;
                    cell.CandyColor = -1;
                    cell.Special=SpecialKind.None;
                    break;
                case Brush.FrozenGrease:
                    cell.Content = CellContent.FrozenGrease;
                    cell.ObstacleState = 1;
                    cell.CandyColor = -1;
                    cell.Special=SpecialKind.None;
                    break;
                case Brush.Oil:
                    cell.Oil = !cell.Oil;
                    break;
                case Brush.RayHorizontal:
                case Brush.RayVertical:
                case Brush.Hammer:
                case Brush.Magnet:
                    cell.Content=CellContent.Candy;cell.ObstacleState=0;cell.CandyColor=-1;
                    cell.Special=brush==Brush.RayHorizontal?SpecialKind.RayHorizontal:brush==Brush.RayVertical?SpecialKind.RayVertical:
                        brush==Brush.Hammer?SpecialKind.Solvent:SpecialKind.Magnet;
                    break;
            }
            RemoveIfDefault(cell);
        }

        private LevelCellData AddCell(int x, int y)
        {
            LevelCellData cell = new() { X = x, Y = y };
            level.Cells.Add(cell);
            return cell;
        }

        private LevelCellData FindCell(int x, int y)
        {
            if (level.Cells == null) return null;
            return level.Cells.Find(item => item != null && item.X == x && item.Y == y);
        }

        private LevelCellData FindMotorAnchorCovering(int x, int y)
        {
            if (level.Cells == null) return null;
            return level.Cells.Find(item => item != null && item.Content == CellContent.Motor &&
                x >= item.X && x < item.X + (level.IsMotorRotated(item)?2:3) &&
                y >= item.Y && y < item.Y + (level.IsMotorRotated(item)?3:2));
        }

        private void RemoveIfDefault(LevelCellData cell)
        {
            if (cell.Content == CellContent.Candy && !cell.Oil && cell.CandyColor < 0&&cell.Special==SpecialKind.None)
                level.Cells.Remove(cell);
        }

        private static string CellLabel(LevelCellData cell)
        {
            if (cell == null) return "·";
            if(cell.Special!=SpecialKind.None)return cell.Special switch
            {SpecialKind.RayHorizontal=>"→",SpecialKind.RayVertical=>"↑",SpecialKind.Solvent=>"Ç",_=>"Mık"};
            string content = cell.Content switch
            {
                CellContent.Bolt => $"B{cell.ObstacleState}",
                CellContent.Motor => $"M{cell.ObstacleState}",
                CellContent.FrozenGrease => "D",
                CellContent.Empty => "X",
                _ => cell.CandyColor >= 0 ? $"Ş{cell.CandyColor}" : "·"
            };
            return cell.Oil ? $"{content}\nY" : content;
        }

        private static Color CellColor(LevelCellData cell)
        {
            if (cell == null) return new Color(0.72f, 0.72f, 0.72f);
            if (cell.Content == CellContent.Bolt) return new Color(0.75f, 0.38f, 0.15f);
            if (cell.Content == CellContent.Motor) return new Color(0.38f, 0.42f, 0.46f);
            if (cell.Content == CellContent.FrozenGrease) return new Color(0.45f, 0.85f, 1f);
            if (cell.Oil) return new Color(0.40f, 0.28f, 0.12f);
            return new Color(0.72f, 0.72f, 0.72f);
        }

        private void Validate()
        {
            validation = LevelValidator.Validate(level);
            Repaint();
        }

        private void Load(string id, bool showDialog)
        {
            string safeId = SafeId(id);
            string path = Path.Combine(LevelDirectory, safeId + ".json");
            if (!File.Exists(path))
            {
                if (showDialog) EditorUtility.DisplayDialog("Bölüm bulunamadı", path, "Tamam");
                level = LevelRepository.CreatePrototype();
                level.Id = safeId;
            }
            else
            {
                level = JsonUtility.FromJson<LevelDefinition>(File.ReadAllText(path));
                level.Cells ??= new List<LevelCellData>();
            }
            Validate();
        }

        private bool Save()
        {
            level.Id = SafeId(level.Id);
            Validate();
            if (validation.Exists(message => message.Severity == LevelValidationSeverity.Error))
            {
                EditorUtility.DisplayDialog("Kaydedilemedi", "Kırmızı doğrulama hatalarını düzelt.", "Tamam");
                return false;
            }
            Directory.CreateDirectory(LevelDirectory);
            string path = Path.Combine(LevelDirectory, level.Id + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(level, true), Encoding.UTF8);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ShowNotification(new GUIContent($"Kaydedildi: {level.Id}"));
            return true;
        }

        private void PlayThisLevel()
        {
            if (!Save()) return;
            PlayerPrefs.SetString(LevelRepository.SelectedLevelKey, level.Id);
            int levelIndex = Array.IndexOf(LevelRepository.PrototypeLevelIds, level.Id);
            if (levelIndex >= 0)
                PlayerPrefs.SetInt(LevelRepository.HighestUnlockedLevelKey,
                    Mathf.Max(PlayerPrefs.GetInt(LevelRepository.HighestUnlockedLevelKey, 0), levelIndex));
            PlayerPrefs.Save();
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }

        private static string SafeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return LevelRepository.DefaultLevelId;
            StringBuilder builder = new();
            foreach (char character in value.Trim())
                if (char.IsLetterOrDigit(character) || character is '_' or '-') builder.Append(character);
            return builder.Length == 0 ? LevelRepository.DefaultLevelId : builder.ToString();
        }
    }
}
