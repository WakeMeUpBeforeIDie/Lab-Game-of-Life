using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;

        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }

        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    public class Pattern
    {
        public string name;
        public string image;
        public int count;
        public int size;

        public Pattern(string _name, string _image, int _size)
        {
            name = _name;
            image = _image;
            size = _size;
        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        public int Generation { get; set; }
        public List<string> states = new List<string>();
        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }
        public Board(int width, int height, int cellSize, double liveDensity = 0.1)
        {
            CellSize = cellSize;
            Generation = 0;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }
        private readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }
        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
            Generation++;
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }
        public int GetAliveCount()
        {
            int count = 0;
            foreach (var cell in Cells)
                if (cell.IsAlive) count++;
            return count;
        }
        public void SaveToFile(string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                for (int row = 0; row < Rows; row++)
                {
                    for (int col = 0; col < Columns; col++)
                        writer.Write(Cells[col, row].IsAlive ? '*' : ' ');
                    writer.WriteLine();
                }
            }
        }
        public void LoadFigure(string filename)
        {
            foreach (var cell in Cells)
                cell.IsAlive = false;

            using (var reader = new StreamReader(filename))
            {
                for (int row = 0; row < Rows; row++)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    for (int col = 0; col < Columns && col < line.Length; col++)
                    {
                        char c = line[col];
                        if (c == '*')
                            Cells[col, row].IsAlive = true;
                    }
                }
            }
            Generation = 0;
        }
        public void FindPattern(Pattern pattern)
        {
            pattern.count = 0;
            for (int row = 0; row <= Rows - pattern.size; row++)
            {
                for (int col = 0; col <= Columns - pattern.size; col++)
                {
                    if (CheckPattern(pattern, row, col))
                        pattern.count++;
                }
            }
        }
        private bool CheckPattern(Pattern pattern, int row, int col)
        {
            for (int i = 0; i < pattern.size; i++)
            {
                for (int j = 0; j < pattern.size; j++)
                {
                    int x = col + j;
                    int y = row + i;
                    char expected = pattern.image[i * pattern.size + j];
                    bool cellAlive = Cells[x, y].IsAlive;

                    if (expected == '*' && !cellAlive) return false;
                    if (expected == '.' && cellAlive) return false;
                }
            }
            return true;
        }
        public bool CheckStable()
        {
            string str = "";
            foreach (var cell in Cells)
                str += cell.IsAlive ? "*" : " ";

            states.Add(str);
            if (states.Count > 3) states.RemoveAt(0);
            return states.Count > 2 && states.Take(states.Count - 1).Contains(str);
        }
    }

    public class Graph
    {
        public static Dictionary<int, int> CountAlive(double density, int width, int height)
        {
            var res = new Dictionary<int, int>();
            var board = new Board(width, height, 1, density);
            int gen = 0;

            while (true)
            {
                if (gen % 20 == 0)
                    res.Add(gen, board.GetAliveCount());
                if (board.CheckStable())
                    break;
                board.Advance();
                gen++;
                if (gen > 500) break;
            }
            return res;
        }
        public static void CreateGraph(int width, int height)
        {
            try
            {
                if (!Directory.Exists("Data"))
                    Directory.CreateDirectory("Data");

                var plot = new ScottPlot.Plot();
                plot.XLabel("Поколение");
                plot.YLabel("Количество живых клеток");
                plot.Title("Переход в стабильное состояние");

                Random rnd = new Random();
                List<double> densities = new List<double>() { 0.2, 0.3, 0.4, 0.5, 0.6, 0.7 };

                using (var writer = new StreamWriter("Data/data.txt"))
                {
                    writer.WriteLine("Density, Generation, Alive ones");

                    foreach (var density in densities)
                    {
                        var data = CountAlive(density, width, height);

                        foreach (var kvp in data)
                            writer.WriteLine($"{density},{kvp.Key},{kvp.Value}");

                        if (data.Count > 0)
                        {
                            var scatter = plot.Add.Scatter(data.Keys.ToArray(), data.Values.ToArray());
                            scatter.Label = $"Плотность {density}";
                            scatter.Color = new ScottPlot.Color(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                        }
                    }
                }
                plot.ShowLegend();
                plot.SavePng("Data/plot.png", 1920, 1080);
                Console.WriteLine("График сохранён в Data/plot.png");
                Console.WriteLine("Данные сохранены в Data/data.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка построения графика: {ex.Message}");
            }
        }
    }
    public class Settings
    {
        public int Width { get; set; } = 50;
        public int Height { get; set; } = 20;
        public int CellSize { get; set; } = 1;
        public int DelayMs { get; set; } = 500;
        public double LiveDensity { get; set; } = 0.5;
        public int MaxGenerations { get; set; } = 500;
        public void Save(string filename = "settings.json")
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filename, json);
        }
        public static Settings Load(string filename = "settings.json")
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                return JsonSerializer.Deserialize<Settings>(json);
            }
            return new Settings();
        }
    }
    class Program
    {
        static Board board;
        static Settings settings;
        static bool autoMode = false;
        static bool exitRequested = false;
        static Pattern block = new Pattern("Блок", "****", 2);
        static Pattern hive = new Pattern("Улей", ".**.*..*.**.....", 4);
        static Pattern boat = new Pattern("Лодка", ".*.*.*.**", 3);
        static Pattern loaf = new Pattern("Каравай", ".**.*..*.*.*..*.", 4);
        static void MapTheGeneration()
        {
            Console.Clear();
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                    Console.Write(board.Cells[col, row].IsAlive ? '*' : ' ');
                Console.WriteLine();
            }
            Console.WriteLine($"Поколение {board.Generation},  живо {board.GetAliveCount()}");
        }
        static void ResetBoard()
        {
            board = new Board(settings.Width, settings.Height, settings.CellSize, settings.LiveDensity);
        }
        static void ShowMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("1. Загрузить фигуру из файла");
                Console.WriteLine("2. Случайное поле");
                Console.WriteLine("3. Создать block");
                Console.WriteLine("4. Создать hive");
                Console.WriteLine("5. Создать boat");
                Console.WriteLine("6. Создать loaf");
                Console.WriteLine("7. Посчитать кол-во элементов");
                Console.WriteLine("8. Исследование стабильности");
                Console.WriteLine("9. Построить график");
                Console.WriteLine("10. Настройки");
                Console.WriteLine("11. Выход");
                Console.Write("\nВыбор: ");
                string choice = Console.ReadLine().ToUpper();
                switch (choice)
                {
                    case "1": LoadFigureDialog(); break;
                    case "2": CreateRandom(); break;
                    case "3": CreateBlock(); break;
                    case "4": Createhive(); break;
                    case "5": CreateBoat(); break;
                    case "6": CreateLoaf(); break;
                    case "7": AnalyzeAndCount(); break;
                    case "8": ResearchStability(); break;
                    case "9": CreateGraph(); break;
                    case "10": EditSettings(); break;
                    case "11": return;
                }
            }
        }
        static void CreateRandom()
        {
            ResetBoard();
            Console.WriteLine($"Случайное поле. Плотность: {settings.LiveDensity:P0}");
            Console.ReadKey();
            RunSimulation();
        }
        static void LoadFigureDialog()
        {
            Console.Write("Введите имя файла: ");
            string filename = Console.ReadLine();

            if (!File.Exists(filename))
            {
                Console.WriteLine("Файл не найден.");
                Console.ReadKey();
                return;
            }
            ResetBoard();
            board.LoadFigure(filename);
            Console.WriteLine($"Загружена фигура. Живых клеток: {board.GetAliveCount()}");
            Console.ReadKey();
            RunSimulation();
        }
        static void CreateBlock()
        {
            ResetBoard();
            foreach (var cell in board.Cells) cell.IsAlive = false;

            int cx = board.Columns / 2;
            int cy = board.Rows / 2;

            board.Cells[cx, cy].IsAlive = true;
            board.Cells[cx + 1, cy].IsAlive = true;
            board.Cells[cx, cy + 1].IsAlive = true;
            board.Cells[cx + 1, cy + 1].IsAlive = true;

            Console.WriteLine("Block created");
            Console.ReadKey();
            RunSimulation();
        }
        static void Createhive()
        {
            ResetBoard();
            foreach (var cell in board.Cells) cell.IsAlive = false;

            int cx = board.Columns / 2;
            int cy = board.Rows / 2;

            board.Cells[cx + 1, cy].IsAlive = true;
            board.Cells[cx + 2, cy].IsAlive = true;
            board.Cells[cx, cy + 1].IsAlive = true;
            board.Cells[cx + 3, cy + 1].IsAlive = true;
            board.Cells[cx + 1, cy + 2].IsAlive = true;
            board.Cells[cx + 2, cy + 2].IsAlive = true;

            Console.WriteLine("Hive created");
            Console.ReadKey();
            RunSimulation();
        }
        static void CreateBoat()
        {
            ResetBoard();
            foreach (var cell in board.Cells) cell.IsAlive = false;

            int cx = board.Columns / 2;
            int cy = board.Rows / 2;

            board.Cells[cx + 1, cy].IsAlive = true;
            board.Cells[cx, cy + 1].IsAlive = true;
            board.Cells[cx + 2, cy + 1].IsAlive = true;
            board.Cells[cx + 1, cy + 2].IsAlive = true;
            board.Cells[cx + 2, cy + 2].IsAlive = true;

            Console.WriteLine("Boat created");
            Console.ReadKey();
            RunSimulation();
        }
        static void CreateLoaf()
        {
            ResetBoard();
            foreach (var cell in board.Cells) cell.IsAlive = false;

            int cx = board.Columns / 2;
            int cy = board.Rows / 2;

            board.Cells[cx + 1, cy].IsAlive = true;
            board.Cells[cx + 2, cy].IsAlive = true;
            board.Cells[cx, cy + 1].IsAlive = true;
            board.Cells[cx + 3, cy + 1].IsAlive = true;
            board.Cells[cx + 1, cy + 2].IsAlive = true;
            board.Cells[cx + 3, cy + 2].IsAlive = true;
            board.Cells[cx + 2, cy + 3].IsAlive = true;
            Console.WriteLine("Loaf created");
            Console.ReadKey();
            RunSimulation();
        }
        static void AnalyzeAndCount()
        {
            if (board == null)
            {
                Console.WriteLine("Сначала нужно создать начальную колонию");
                Console.ReadKey();
                return;
            }
            block.count = 0;
            hive.count = 0;
            boat.count = 0;
            loaf.count = 0;
            board.FindPattern(block);
            board.FindPattern(hive);
            board.FindPattern(boat);
            board.FindPattern(loaf);
            Console.Clear();
            Console.WriteLine("Анализ\n");
            Console.WriteLine($"Всего живо: {board.GetAliveCount()}\n");
            Console.WriteLine("Найдено\n");
            Console.WriteLine($"Blocks: {block.count} шт.");
            Console.WriteLine($"Hives: {hive.count} шт.");
            Console.WriteLine($"Boats: {boat.count} шт.");
            Console.WriteLine($"Loafs: {loaf.count} шт.");
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }
        static void ResearchStability()
        {
            Console.Clear();
            Console.WriteLine("Стабильность\n");
            if (!Directory.Exists("Stability"))
                Directory.CreateDirectory("Stability");
            double[] densities = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7 };
            using (var writer = new StreamWriter("Stability/stability.txt"))
            {
                writer.WriteLine("Density, StableGeneration");

                foreach (var density in densities)
                {
                    Console.Write($"Плотность {density:P0}... ");
                    var testBoard = new Board(settings.Width, settings.Height, settings.CellSize, density);
                    int stableGen = -1;

                    for (int gen = 0; gen < 500; gen++)
                    {
                        testBoard.Advance();
                        if (testBoard.CheckStable())
                        {
                            stableGen = gen;
                            break;
                        }
                    }

                    writer.WriteLine($"{density:F2},{stableGen}");
                    Console.WriteLine($" стабилизация на {stableGen} поколении");
                }
            }
            Console.WriteLine("\nРезультаты сохранены");
            Console.ReadKey();
        }
        static void CreateGraph()
        {
            Console.Clear();
            Console.WriteLine("Построение графика\n");
            Graph.CreateGraph(settings.Width, settings.Height);
            Console.ReadKey();
        }
        static void EditSettings()
        {
            Console.Clear();
            Console.WriteLine("Настройки\n");
            Console.WriteLine($"1. Ширина: {settings.Width}");
            Console.WriteLine($"2. Высота: {settings.Height}");
            Console.WriteLine($"3. Задержка: {settings.DelayMs}");
            Console.WriteLine($"4. Плотность: {settings.LiveDensity:P0}");
            Console.WriteLine($"5. Лимит поколений: {settings.MaxGenerations}");
            Console.Write("\nВыберите параметр (1-5): ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    Console.Write("Новая ширина: ");
                    settings.Width = int.Parse(Console.ReadLine());
                    break;
                case "2":
                    Console.Write("Новая высота: ");
                    settings.Height = int.Parse(Console.ReadLine());
                    break;
                case "3":
                    Console.Write("Новая задержка (мс): ");
                    settings.DelayMs = int.Parse(Console.ReadLine());
                    break;
                case "4":
                    Console.Write("Новая плотность (0-1): ");
                    settings.LiveDensity = double.Parse(Console.ReadLine());
                    break;
                case "5":
                    Console.Write("Макс. поколений: ");
                    settings.MaxGenerations = int.Parse(Console.ReadLine());
                    break;
            }
            settings.Save();
            Console.WriteLine("\nНастройки сохранены.");
            Console.ReadKey();
        }
        static void RunSimulation()
        {
            autoMode = false;
            exitRequested = false;
            while (!exitRequested && board.Generation < settings.MaxGenerations)
            {
                MapTheGeneration();
                Console.WriteLine("\nSpace to step  S to save  A to count figures  W to auto-mode  Esc to quit");
                if (autoMode)
                {
                    Thread.Sleep(settings.DelayMs);
                    board.Advance();
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if (key == ConsoleKey.Escape) exitRequested = true;
                        if (key == ConsoleKey.W) autoMode = false;
                    }
                }
                else
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.Spacebar:
                            board.Advance();
                            break;
                        case ConsoleKey.W:
                            autoMode = true;
                            break;
                        case ConsoleKey.S:
                            string filename = $"save_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                            board.SaveToFile(filename);
                            Console.WriteLine($"Сохранено в {filename}");
                            Thread.Sleep(500);
                            break;
                        case ConsoleKey.A:
                            AnalyzeAndCount();
                            break;
                        case ConsoleKey.Escape:
                            exitRequested = true;
                            break;
                    }
                }
            }
            if (board.Generation >= settings.MaxGenerations)
            {
                Console.WriteLine($"\nДостигнут лимит поколений ({settings.MaxGenerations})");
                Console.ReadKey();
            }
        }
        static void CreateExampleFigures()
        {
            if (!File.Exists("figures/block.txt"))
                File.WriteAllText("figures/block.txt", "**\n**");

            if (!File.Exists("figures/hive.txt"))
                File.WriteAllText("figures/hive.txt", ".**.\n*..*\n.**.");
        }
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            settings = Settings.Load();
            if (!Directory.Exists("figures"))
                Directory.CreateDirectory("figures");
            CreateExampleFigures();
            ShowMenu();
        }
    }
}
