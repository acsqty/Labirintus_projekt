using System;
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
        bool isHungarian = true;

        public MainWindow()
        {
            InitializeComponent();
            map = new char[rows, cols];
            InitMap(rows, cols);
            BuildPalette();
            DrawMap();
            RefreshLabels();
        }

        void RefreshLabels()
        {
            if (isHungarian)
            {
                menuFajl.Header = "Fájl";
                menuUj.Header = "Új";
                menuMegnyitas.Header = "Megnyitás";
                menuMentes.Header = "Mentés";
                menuKilepes.Header = "Kilépés";
                menuNyelv.Header = "Nyelv";
                txtPaletteTitle.Text = "Karakterek:";
                txtMapSizeLabel.Text = "Térkép mérete:";
                txtRowLabel.Text = "Sor:";
                txtColLabel.Text = "Oszlop:";
                btnNewMap.Content = "Új térkép";
                txtInfoTitle.Text = "Információ:";
            }
            else
            {
                menuFajl.Header = "File";
                menuUj.Header = "New";
                menuMegnyitas.Header = "Open";
                menuMentes.Header = "Save";
                menuKilepes.Header = "Exit";
                menuNyelv.Header = "Language";
                txtPaletteTitle.Text = "Characters:";
                txtMapSizeLabel.Text = "Map size:";
                txtRowLabel.Text = "Row:";
                txtColLabel.Text = "Column:";
                btnNewMap.Content = "New map";
                txtInfoTitle.Text = "Information:";
            }
            UpdateInfo();
        }

        void InitMap(int r, int c)
        {
            rows = r;
            cols = c;
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
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 16,
                    Background = GetBg(c),
                    Foreground = GetFg(c),
                    Tag = c
                };
                btn.Click += (s, e) =>
                {
                    selectedChar = (char)((Button)s).Tag;
                    txtStatus.Text = $"{(isHungarian ? "Kiválasztott" : "Selected")}: '{selectedChar}'";
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
                        Background = GetBg(map[r, c]),
                        Foreground = GetFg(map[r, c]),
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1)
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
            if (c == ROOM || c == FILL) return Brushes.Black;
            return Brushes.Black;
        }

        void UpdateInfo()
        {
            int roomCount = 0;
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    if (map[i, j] == ROOM) roomCount++;
            
            string roomText = isHungarian ? "Termek" : "Rooms";
            txtRooms.Text = $"{roomText}: {roomCount}";
            txtStatus.Text = $"{roomText}: {roomCount}";
        }

        void SaveMap(string fileName)
        {
            if (!fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                fileName += ".txt";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    sb.Append(map[i, j]);
                if (i < rows - 1)
                    sb.AppendLine();
            }
            File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
        }

        void LoadMap(string path)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0) return;

            rows = lines.Length;
            cols = lines.Max(l => l.Length);
            map = new char[rows, cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    map[i, j] = (j < lines[i].Length) ? lines[i][j] : FILL;

            txtRows.Text = rows.ToString();
            txtCols.Text = cols.ToString();
            DrawMap();
        }

        void MenuUj_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtRows.Text, out int r) || r < 3) r = 15;
            if (!int.TryParse(txtCols.Text, out int c) || c < 3) c = 20;
            InitMap(r, c);
            DrawMap();
        }

        void BtnNewMap_Click(object sender, RoutedEventArgs e) => MenuUj_Click(sender, e);

        void MenuMegnyitas_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog 
            { 
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true) 
                LoadMap(dlg.FileName);
        }

        void MenuMentes_Click(object sender, RoutedEventArgs e)
        {
            int roomCount = 0;
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    if (map[i, j] == ROOM) roomCount++;

            if (roomCount == 0)
            {
                string errMsg = isHungarian ? "Hiba: A térkép nem tartalmaz termet!" : "Error: Map contains no rooms!";
                MessageBox.Show(errMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                DefaultExt = ".txt"
            };

            if (dlg.ShowDialog() == true)
            {
                SaveMap(dlg.FileName);
                string successMsg = isHungarian ? "Térkép sikeresen mentve!" : "Map saved successfully!";
                MessageBox.Show(successMsg);
            }
        }

        void MenuKilepes_Click(object sender, RoutedEventArgs e) => Close();

        void MenuMagyar_Click(object sender, RoutedEventArgs e)
        {
            isHungarian = true;
            RefreshLabels();
        }

        void MenuEnglish_Click(object sender, RoutedEventArgs e)
        {
            isHungarian = false;
            RefreshLabels();
        }
    }
}