using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorkshopMatch
{
    [Serializable]
    public sealed class LevelCellData
    {
        public int X;
        public int Y;
        public CellContent Content = CellContent.Candy;
        public int ObstacleState;
        public bool Oil;
        public int CandyColor = -1;
        public SpecialKind Special;
        // 0 keeps legacy level rotation; 1 horizontal, 2 vertical.
        public int MotorOrientation;

        public LevelCellData Copy()
        {
            return new LevelCellData
            {
                X = X,
                Y = Y,
                Content = Content,
                ObstacleState = ObstacleState,
                Oil = Oil,
                CandyColor = CandyColor,
                Special = Special,
                MotorOrientation = MotorOrientation
            };
        }
    }

    [Serializable]
    public sealed class LevelDefinition
    {
        public int Version = 1;
        public string Id = "level_001";
        public string DisplayTitle = "T06 Mekanik Taslağı";
        public bool IsDraft = true;
        public int Width = 8;
        public int Height = 8;
        public int Moves = 18;
        public int ColorCount = 5;
        public int SpawnSeed = 24092026;
        public string ChapterMachineId = "prototype";
        public bool MotorRotated;
        public List<LevelCellData> Cells = new();

        public bool IsMotorRotated(LevelCellData cell)=>cell.MotorOrientation==0?MotorRotated:cell.MotorOrientation==2;

        public LevelDefinition Copy()
        {
            LevelDefinition copy = (LevelDefinition)MemberwiseClone();
            copy.Cells = new List<LevelCellData>();
            if (Cells != null)
                foreach (LevelCellData cell in Cells)
                    if (cell != null) copy.Cells.Add(cell.Copy());
            return copy;
        }
    }

    public enum LevelValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public readonly struct LevelValidationMessage
    {
        public LevelValidationMessage(LevelValidationSeverity severity, string text)
        {
            Severity = severity;
            Text = text;
        }

        public LevelValidationSeverity Severity { get; }
        public string Text { get; }
        public override string ToString() => $"{Severity}: {Text}";
    }

    public static class LevelRepository
    {
        public const string SelectedLevelKey = "WorkshopMatch.SelectedLevel";
        public const string HighestUnlockedLevelKey = "WorkshopMatch.HighestUnlockedLevel";
        public const string DefaultLevelId = "level_001";
        public static readonly string[] PrototypeLevelIds =
        {
            "level_001", "level_002", "level_003", "level_004", "level_005",
            "level_006", "level_007", "level_008", "level_009", "level_010",
            "level_011", "level_012", "level_013", "level_014", "level_015",
            "level_016", "level_017", "level_018", "level_019", "level_020"
        };

        public static string SelectedLevelId
        {
            get
            {
                string id = PlayerPrefs.GetString(SelectedLevelKey, DefaultLevelId);
                return string.IsNullOrWhiteSpace(id) ? DefaultLevelId : id;
            }
        }

        public static LevelDefinition LoadSelected() => Load(SelectedLevelId);

        public static void Select(string id)
        {
            PlayerPrefs.SetString(SelectedLevelKey, string.IsNullOrWhiteSpace(id) ? DefaultLevelId : id);
            PlayerPrefs.Save();
        }

        public static LevelDefinition Load(string id)
        {
            string safeId = string.IsNullOrWhiteSpace(id) ? DefaultLevelId : id;
            TextAsset asset = Resources.Load<TextAsset>($"WorkshopMatch/Levels/{safeId}");
            if (asset == null)
            {
                Debug.LogWarning($"Level '{safeId}' bulunamadı; geçici T06 taslağı kullanılıyor.");
                return CreatePrototype();
            }

            LevelDefinition definition = JsonUtility.FromJson<LevelDefinition>(asset.text);
            if (definition == null)
                throw new InvalidOperationException($"Level '{safeId}' JSON olarak okunamadı.");
            definition.Cells ??= new List<LevelCellData>();
            return definition;
        }

        public static Match3BoardModel BuildBoard(LevelDefinition definition, Action<SpecialKind> specialProduced = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            Match3BoardModel board = new(definition.Width, definition.Height, definition.ColorCount,
                definition.SpawnSeed, definition.Moves, specialProduced);
            if (definition.Cells != null)
            {
                foreach (LevelCellData data in definition.Cells)
                {
                    if (data == null || data.X < 0 || data.Y < 0 || data.X >= definition.Width || data.Y >= definition.Height)
                        continue;
                    Match3BoardModel.Cell cell = new(data.X, data.Y);
                    if (data.Content == CellContent.Motor)
                    {
                        int motorWidth = definition.IsMotorRotated(data) ? 2 : 3;
                        int motorHeight = definition.IsMotorRotated(data) ? 3 : 2;
                        board.PlaceMotor(cell,Math.Min(motorWidth,definition.Width-data.X),Math.Min(motorHeight,definition.Height-data.Y),Math.Max(1,data.ObstacleState));
                    }
                    else if (data.Content is CellContent.Bolt or CellContent.FrozenGrease)
                        board.PlaceObstacle(cell, data.Content, Math.Max(1, data.ObstacleState));
                    else if (data.CandyColor >= 0 && data.CandyColor < definition.ColorCount)
                        board.DebugSetCandy(cell, data.CandyColor);
                    if(data.Content==CellContent.Candy&&data.Special!=SpecialKind.None)
                        board.DebugPlaceSpecial(cell,data.Special,data.CandyColor>=0?data.CandyColor:board.GetColor(data.X,data.Y));
                    if (data.Oil)
                        board.SetOil(cell);
                }
            }
            if(definition.Id=="level_001")board.ConfigureMoveGoal(10);
            board.SealObjectives();
            return board;
        }

        public static LevelDefinition CreatePrototype()
        {
            LevelDefinition definition = new();
            definition.Cells.Add(new LevelCellData { X = 1, Y = 6, Content = CellContent.Bolt, ObstacleState = 2 });
            definition.Cells.Add(new LevelCellData { X = 3, Y = 4, Content = CellContent.Motor, ObstacleState = 5 });
            definition.Cells.Add(new LevelCellData { X = 6, Y = 2, Content = CellContent.FrozenGrease, ObstacleState = 1, Oil = true });
            definition.Cells.Add(new LevelCellData { X = 2, Y = 2, Content = CellContent.Candy, Oil = true });
            definition.Cells.Add(new LevelCellData { X = 5, Y = 5, Content = CellContent.Candy, Oil = true });
            return definition;
        }
    }

    public static class LevelValidator
    {
        public static List<LevelValidationMessage> Validate(LevelDefinition definition)
        {
            List<LevelValidationMessage> messages = new();
            if (definition == null)
            {
                messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "Bölüm verisi boş."));
                return messages;
            }

            if (definition.Version != 1)
                messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"Desteklenmeyen veri sürümü: {definition.Version}."));
            if (string.IsNullOrWhiteSpace(definition.Id))
                messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "Bölüm kimliği boş olamaz."));
            if (definition.Width < 3 || definition.Height < 3)
                messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "Tahta en az 3×3 olmalı."));
            if (definition.Width > 12 || definition.Height > 14)
                messages.Add(new LevelValidationMessage(LevelValidationSeverity.Warning, "Büyük tahta telefon ekranında okunaksız olabilir."));
            if (definition.Moves <= 0)
                messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "Hamle sayısı sıfırdan büyük olmalı."));
            if (definition.ColorCount < 3 || definition.ColorCount > 5)
                messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "Renk sayısı 3–5 arasında olmalı."));

            HashSet<long> occupied = new();
            HashSet<long> recorded = new();
            HashSet<long> blocking = new();
            int motors = 0;
            int bolts = 0;
            int oils = 0;
            if (definition.Cells != null)
            {
                foreach (LevelCellData cell in definition.Cells)
                {
                    if (cell == null)
                    {
                        messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, "Boş hücre kaydı bulundu."));
                        continue;
                    }
                    if (cell.X < 0 || cell.Y < 0 || cell.X >= definition.Width || cell.Y >= definition.Height)
                    {
                        messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"({cell.X},{cell.Y}) tahta dışında."));
                        continue;
                    }
                    long key = ((long)cell.X << 32) | (uint)cell.Y;
                    bool overlay=cell.Content==CellContent.Candy&&cell.CandyColor<0&&cell.Special==SpecialKind.None;
                    if (!recorded.Add(key)||(!overlay&&!occupied.Add(key)))
                        messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"({cell.X},{cell.Y}) konumunda çakışan kayıtlar var."));
                    if (cell.Content == CellContent.Empty)
                        messages.Add(new LevelValidationMessage(LevelValidationSeverity.Warning, $"({cell.X},{cell.Y}) Empty; silgiyle kaldırılabilir."));
                    if (cell.Content == CellContent.Motor)
                    {
                        motors++;
                        int motorWidth = definition.IsMotorRotated(cell) ? 2 : 3;
                        int motorHeight = definition.IsMotorRotated(cell) ? 3 : 2;
                        if (cell.X + motorWidth > definition.Width || cell.Y + motorHeight > definition.Height)
                            messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error,
                                $"({cell.X},{cell.Y}) motor ankrajı {motorWidth}×{motorHeight} alanıyla tahta dışına taşıyor."));
                        for (int motorY = 0; motorY < motorHeight && cell.Y + motorY < definition.Height; motorY++)
                        {
                            for (int motorX = 0; motorX < motorWidth && cell.X + motorX < definition.Width; motorX++)
                            {
                                long motorKey = ((long)(cell.X + motorX) << 32) | (uint)(cell.Y + motorY);
                                blocking.Add(motorKey);
                                if ((motorX != 0 || motorY != 0) && !occupied.Add(motorKey))
                                    messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error,
                                        $"Motor alanı ({cell.X + motorX},{cell.Y + motorY}) üzerindeki başka kayıtla çakışıyor."));
                            }
                        }
                    }
                    if (cell.Content is CellContent.Bolt or CellContent.Motor or CellContent.FrozenGrease)
                        blocking.Add(key);
                    if (cell.Content == CellContent.Bolt) bolts++;
                    if (cell.Oil) oils++;
                    if (cell.Content != CellContent.Candy && cell.ObstacleState <= 0)
                        messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"({cell.X},{cell.Y}) engel dayanıklılığı sıfır."));
                    if (cell.CandyColor >= definition.ColorCount)
                        messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"({cell.X},{cell.Y}) şeker rengi renk sayısının dışında."));
                    if(!Enum.IsDefined(typeof(SpecialKind),cell.Special)||(cell.Special!=SpecialKind.None&&cell.Content!=CellContent.Candy))
                        messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error,$"({cell.X},{cell.Y}) özel parça türü/geçtiği hücre hatalı."));
                }
            }
            if (motors == 0)
                messages.Add(new LevelValidationMessage(LevelValidationSeverity.Warning, "Motor hedefi yok."));
            if (bolts == 0 && oils == 0 && motors == 0)
                messages.Add(new LevelValidationMessage(LevelValidationSeverity.Warning, "Bölümde hedef yok."));

            if (definition.Cells != null)
            {
                foreach (LevelCellData cell in definition.Cells)
                {
                    if (cell == null || cell.Content is not (CellContent.Bolt or CellContent.Motor or CellContent.FrozenGrease))
                        continue;
                    if (!HasReachableNeighbour(cell.X, cell.Y, definition.Width, definition.Height, blocking))
                        messages.Add(new LevelValidationMessage(LevelValidationSeverity.Warning,
                            $"({cell.X},{cell.Y}) hedefinin eşleşmeyle vurulabilecek açık komşusu yok."));
                }
            }

            bool dimensionsUsable = definition.Width >= 3 && definition.Height >= 3 &&
                                    definition.ColorCount is >= 3 and <= 5 && definition.Moves > 0;
            bool hasStructuralError = messages.Exists(message => message.Severity == LevelValidationSeverity.Error);
            if (dimensionsUsable && !hasStructuralError)
            {
                try
                {
                    Match3BoardModel board = BuildUnsealedBoard(definition);
                    if (!board.IsStable)
                        messages.Add(new LevelValidationMessage(LevelValidationSeverity.Warning, "Başlangıç diziliminde otomatik eşleşme var."));
                    if (!board.TryFindValidSwap(out _, out _))
                        messages.Add(new LevelValidationMessage(LevelValidationSeverity.Warning, "Başlangıç diziliminde geçerli hamle bulunamadı."));
                }
                catch (Exception exception)
                {
                    messages.Add(new LevelValidationMessage(LevelValidationSeverity.Error, $"Tahta kurulamadı: {exception.Message}"));
                }
            }

            if (messages.Count == 0)
                messages.Add(new LevelValidationMessage(LevelValidationSeverity.Info, "Bölüm verisi geçerli ve oynanabilir görünüyor."));
            return messages;
        }

        private static Match3BoardModel BuildUnsealedBoard(LevelDefinition definition)
        {
            Match3BoardModel board = new(definition.Width, definition.Height, definition.ColorCount,
                definition.SpawnSeed, definition.Moves);
            foreach (LevelCellData data in definition.Cells ?? new List<LevelCellData>())
            {
                Match3BoardModel.Cell cell = new(data.X, data.Y);
                if (data.Content == CellContent.Motor)
                {
                    int motorWidth = definition.IsMotorRotated(data) ? 2 : 3;
                    int motorHeight = definition.IsMotorRotated(data) ? 3 : 2;
                    board.PlaceMotor(cell,Math.Min(motorWidth,definition.Width-data.X),Math.Min(motorHeight,definition.Height-data.Y),data.ObstacleState);
                }
                else if (data.Content is CellContent.Bolt or CellContent.FrozenGrease)
                    board.PlaceObstacle(cell, data.Content, data.ObstacleState);
                else if (data.CandyColor >= 0)
                    board.DebugSetCandy(cell, data.CandyColor);
                if(data.Content==CellContent.Candy&&data.Special!=SpecialKind.None)
                    board.DebugPlaceSpecial(cell,data.Special,data.CandyColor>=0?data.CandyColor:board.GetColor(data.X,data.Y));
                if (data.Oil) board.SetOil(cell);
            }
            return board;
        }

        private static bool HasReachableNeighbour(int x, int y, int width, int height, HashSet<long> blocking)
        {
            int[] deltaX = { -1, 1, 0, 0 };
            int[] deltaY = { 0, 0, -1, 1 };
            for (int i = 0; i < deltaX.Length; i++)
            {
                int nextX = x + deltaX[i];
                int nextY = y + deltaY[i];
                if (nextX < 0 || nextY < 0 || nextX >= width || nextY >= height) continue;
                long key = ((long)nextX << 32) | (uint)nextY;
                if (!blocking.Contains(key)) return true;
            }
            return false;
        }
    }
}
