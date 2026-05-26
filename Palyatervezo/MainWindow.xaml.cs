using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace Palyatervezo
{
    public partial class MainWindow : Window
    {
        static readonly char[] corridorChars = { '╬', '═', '╦', '╩', '║', '╣', '╠', '╗', '╝', '╚', '╔' };
        const char FILL = '.';
        const char ROOM = '█';

        char[,] map;
        int rows = 15;
        int cols = 20;
        char selectedChar = FILL;
        Dictionary<string, string> lang = new Dictionary<string, string>();
        Button? selectedButton = null;

        public MainWindow()
        {
            InitializeComponent();
            map = new char[rows, cols];
            SetLanguage(true);
            InitMap(rows, cols);
            BuildPalette();
            DrawMap();
        }

        void SetLanguage(bool hungarian)
        {
            if (hungarian)
            {
                lang["file"] = "Fájl"; lang["new"] = "Új"; lang["open"] = "Megnyitás"; lang["save"] = "Mentés";
                lang["exit"] = "Kilépés"; lang["language"] = "Nyelv"; lang["palette"] = "Karakterek:";
                lang["mapsizeLabel"] = "Térkép mérete:"; lang["rowLabel"] = "Sor:"; lang["colLabel"] = "Oszlop:";
                lang["newmap"] = "Új térkép"; lang["info"] = "Információ:"; lang["rooms"] = "Termek:";
                lang["selected"] = "Kiválasztott:"; lang["saveOk"] = "Térkép sikeresen mentve!";
                lang["errRooms"] = "Hiba: A térkép nem tartalmaz termet!";
                lang["savePrompt"] = "Add meg a fájl nevét (kiterjesztés nélkül):";
                lang["saveTitle"] = "Mentés";
                lang["invalidName"] = "Érvénytelen fájlnév!";
            }
            else
            {
                lang["file"] = "File"; lang["new"] = "New"; lang["open"] = "Open"; lang["save"] = "Save";
                lang["exit"] = "Exit"; lang["language"] = "Language"; lang["palette"] = "Characters:";
                lang["mapsizeLabel"] = "Map size:"; lang["rowLabel"] = "Row:"; lang["colLabel"] = "Column:";
                lang["newmap"] = "New map"; lang["info"] = "Information:"; lang["rooms"] = "Rooms:";
                lang["selected"] = "Selected:"; lang["saveOk"] = "Map saved successfully!";
                lang["errRooms"] = "Error: Map contains no rooms!";
                lang["savePrompt"] = "Enter file name (without extension):";
                lang["saveTitle"] = "Save";
                lang["invalidName"] = "Invalid file name!";
            }
            RefreshLabels();
        }

        void RefreshLabels()
        {
            menuFajl.Header = lang["file"]; menuUj.Header = lang["new"]; menuMegnyitas.Header = lang["open"];
            menuMentes.Header = lang["save"]; menuKilepes.Header = lang["exit"]; menuNyelv.Header = lang["language"];
            txtPaletteTitle.Text = lang["palette"]; txtMapSizeLabel.Text = lang["mapsizeLabel"];
            txtRowLabel.Text = lang["rowLabel"]; txtColLabel.Text = lang["colLabel"];
            btnNewMap.Content = lang["newmap"]; txtInfoTitle.Text = lang["info"];
            UpdateInfo();
        }

        void InitMap(int r, int c)
        {
            rows = r; cols = c;
            map = new char[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    map[i, j] = FILL;
        }

        void BuildPalette()
        {
            palettePanel.Children.Clear();
            char[] allChars = new[] { FILL, ROOM }.Concat(corridorChars).ToArray();

            foreach (char c in allChars)
            {
                Button btn = new Button
                {
                    Content = c.ToString(),
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(2),
                    Padding = new Thickness(0),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 16,
                    Background = GetBg(c),
                    Foreground = GetFg(c),
                    Tag = c,
                    BorderBrush = Brushes.DarkGray,
                    BorderThickness = new Thickness(1)
                };
                btn.Click += (s, e) =>
                {
                    if (selectedButton != null)
                    {
                        selectedButton.BorderBrush = Brushes.DarkGray;
                        selectedButton.BorderThickness = new Thickness(1);
                    }

                    selectedChar = (char)((Button)s).Tag;
                    selectedButton = (Button)s;
                    selectedButton.BorderBrush = Brushes.Cyan;
                    selectedButton.BorderThickness = new Thickness(3);
                    txtStatus.Text = $"{lang["selected"]} '{selectedChar}'";
                };
                palettePanel.Children.Add(btn);
            }
        }

        void DrawMap()
        {
            mapGrid.Rows = rows;
            mapGrid.Columns = cols;
            mapGrid.Children.Clear();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    int r = i, c = j;
                    Button btn = new Button
                    {
                        Content = map[r, c].ToString(),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Background = GetBg(map[r, c]),
                        Foreground = GetFg(map[r, c]),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(0),
                        Padding = new Thickness(0),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    btn.Click += (s, e) =>
                    {
                        map[r, c] = selectedChar;
                        btn.Content = selectedChar.ToString();
                        btn.Background = GetBg(selectedChar);
                        btn.Foreground = GetFg(selectedChar);
                        UpdateInfo();
                    };
                    mapGrid.Children.Add(btn);
                }
            }
            UpdateInfo();
        }

        Brush GetBg(char c)
        {
            if (c == ROOM) return new SolidColorBrush(Color.FromRgb(255, 215, 0));
            if (c == FILL) return new SolidColorBrush(Color.FromRgb(30, 30, 30));
            return Brushes.White;
        }

        Brush GetFg(char c)
        {
            if (c == ROOM) return Brushes.Black;
            if (c == FILL) return new SolidColorBrush(Color.FromRgb(80, 80, 80));
            return Brushes.Black;
        }

        void UpdateInfo()
        {
            if (map == null) return;
            int r = GetRoomNumber(map);
            txtRooms.Text = $"{lang["rooms"]} {r}";
            txtStatus.Text = $"{lang["rooms"]} {r}";
        }

        static int GetRoomNumber(char[,] map)
        {
            int count = 0;
            for (int i = 0; i < map.GetLength(0); i++)
                for (int j = 0; j < map.GetLength(1); j++)
                    if (map[i, j] == ROOM) count++;
            return count;
        }

        static int GetSuitableEntrance(char[,] map)
        {
            int r = map.GetLength(0), c = map.GetLength(1), count = 0;
            for (int j = 0; j < c; j++)
            {
                if (corridorChars.Contains(map[0, j])) count++;
                if (corridorChars.Contains(map[r - 1, j])) count++;
            }
            for (int i = 1; i < r - 1; i++)
            {
                if (corridorChars.Contains(map[i, 0])) count++;
                if (corridorChars.Contains(map[i, c - 1])) count++;
            }
            return count;
        }

        string GetSolutionDirectory()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo? dir = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent;
            return dir?.FullName ?? currentDir;
        }

        void SaveMap(string fileName)
        {
            if (!fileName.EndsWith(".sav", StringComparison.OrdinalIgnoreCase))
                fileName += ".sav";

            string solutionDir = GetSolutionDirectory();
            string fullPath = Path.Combine(solutionDir, fileName);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    sb.Append(map[i, j]);
                }
                if (i < rows - 1)
                    sb.AppendLine();
            }
            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        }

        void LoadMap(string path)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) return;

            rows = lines.Length;
            cols = lines.Max(l => l.Length);
            map = new char[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    map[i, j] = (j < lines[i].Length) ? lines[i][j] : FILL;
                }
            }

            txtRows.Text = rows.ToString();
            txtCols.Text = cols.ToString();
            DrawMap();
        }

        void MenuUj_Click(object sender, RoutedEventArgs e) => BtnNewMap_Click(sender, e);

        void BtnNewMap_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtRows.Text, out int r) || r < 3) r = 15;
            if (!int.TryParse(txtCols.Text, out int c) || c < 3) c = 20;
            InitMap(r, c);
            DrawMap();
        }

        void MenuMegnyitas_Click(object sender, RoutedEventArgs e)
        {
            string solutionDir = GetSolutionDirectory();
            var dlg = new OpenFileDialog 
            { 
                Filter = "Pálya fájlok (*.sav;*.txt)|*.sav;*.txt|Minden fájl (*.*)|*.*",
                InitialDirectory = solutionDir
            };
            if (dlg.ShowDialog() == true) LoadMap(dlg.FileName);
        }

        void MenuMentes_Click(object sender, RoutedEventArgs e)
        {
            if (GetRoomNumber(map) == 0)
            {
                MessageBox.Show(lang["errRooms"], "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var inputDialog = new Window
            {
                Title = lang["saveTitle"],
                Width = 350,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(43, 43, 43))
            };

            var sp = new StackPanel { Margin = new Thickness(15) };
            sp.Children.Add(new TextBlock 
            { 
                Text = lang["savePrompt"], 
                Foreground = Brushes.White, 
                Margin = new Thickness(0, 0, 0, 10) 
            });
            
            var txtFileName = new TextBox { Margin = new Thickness(0, 0, 0, 15) };
            sp.Children.Add(txtFileName);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new Button { Content = "OK", Width = 70, Margin = new Thickness(5) };
            var btnCancel = new Button { Content = "Mégse", Width = 70, Margin = new Thickness(5) };
            
            btnOk.Click += (s, ev) => 
            { 
                string fileName = txtFileName.Text.Trim();
                if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    MessageBox.Show(lang["invalidName"], "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                inputDialog.DialogResult = true; 
                inputDialog.Close(); 
            };
            btnCancel.Click += (s, ev) => { inputDialog.Close(); };
            
            btns.Children.Add(btnOk);
            btns.Children.Add(btnCancel);
            sp.Children.Add(btns);

            inputDialog.Content = sp;

            if (inputDialog.ShowDialog() == true)
            {
                string fileName = txtFileName.Text.Trim();
                SaveMap(fileName);
                MessageBox.Show(lang["saveOk"] + $"\n({fileName}.sav)");
            }
        }

        void MenuKilepes_Click(object sender, RoutedEventArgs e) => Close();
        void MenuMagyar_Click(object sender, RoutedEventArgs e) => SetLanguage(true);
        void MenuEnglish_Click(object sender, RoutedEventArgs e) => SetLanguage(false);
    }
}