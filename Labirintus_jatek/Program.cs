using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Labirintus_jatek
{
    internal class Program
    {
        static readonly char[] JaratKarakterek = { '╬', '═', '╦', '╩', '║', '╣', '╠', '╗', '╝', '╚', '╔' };
        static readonly char Terem = '█', Kitolto = '.', JatekosJel = '@';
        static char[,] terkep;
        static bool[,] latogatott;
        static int jatekosX, jatekosY, hatralevIdo = -1;
        static HashSet<string> felfedezettTermek = new HashSet<string>();
        static string terkepNev, terkepTeljesUtvonal;
        static bool magyar = true, fedettMod = false;
        static Stopwatch stopwatch;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Console.SetWindowSize(120, 40);
                Console.SetBufferSize(120, 40);
                Console.CursorVisible = false;
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch { }

            while (true)
            {
                Console.Clear();
                Menu();
            }
        }

        static void Menu()
        {
            Console.WriteLine("╔════════════════════════════════════╗");
            Console.WriteLine(magyar ? "║      LABIRINTUS JÁTÉK          ║" : "║      LABYRINTH GAME            ║");
            Console.WriteLine("╚════════════════════════════════════╝\n");
            Console.WriteLine(magyar ? "  1 - Új játék\n  2 - Betöltés\n  3 - Nyelv (HU/EN)\n  4 - Kilépés" :
                "  1 - New game\n  2 - Load\n  3 - Language\n  4 - Exit");
            Console.Write("\n  > ");

            switch (Console.ReadLine())
            {
                case "1": UjJatek(); break;
                case "2": Betoltes(); break;
                case "3": magyar = !magyar; break;
                case "4": Environment.Exit(0); break;
            }
        }

        static void UjJatek()
        {
            Console.Clear();
            using (OpenFileDialog dialog = new OpenFileDialog 
            { 
                Filter = "Térkép fájlok|*.txt",
                InitialDirectory = Environment.CurrentDirectory 
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                terkepTeljesUtvonal = dialog.FileName;
                terkepNev = Path.GetFileNameWithoutExtension(dialog.FileName);
                TerkepBetolt(dialog.FileName);
            }

            Console.Clear();
            Console.WriteLine(magyar ? "✓ Térkép betöltve!\n" : "✓ Map loaded!\n");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(magyar ? $"  Termek száma: {GetRoomNumber(terkep)}" : $"  Rooms: {GetRoomNumber(terkep)}");
            Console.WriteLine(magyar ? $"  Bejáratok száma: {GetSuitableEntrance(terkep)}" : $"  Entrances: {GetSuitableEntrance(terkep)}");
            Console.WriteLine(magyar ? $"  Érvénytelen karakter: {(IsInvalidElement(terkep) ? "VAN" : "NINCS")}" :
                $"  Invalid chars: {(IsInvalidElement(terkep) ? "YES" : "NO")}");
            var hibasak = GetUnavailableElements(terkep);
            Console.WriteLine(magyar ? $"  Elérhetetlen járatok: {hibasak.Count}" : $"  Unreachable: {hibasak.Count}");
            Console.ResetColor();
            Console.WriteLine("\n" + (magyar ? "  Nyomj billentyűt..." : "  Press key..."));
            Console.ReadKey();

            Console.Clear();
            Console.Write(magyar ? "Időre játék? (1-Igen/2-Nem): " : "Timed? (1-Yes/2-No): ");
            if (Console.ReadLine() == "1")
            {
                Console.Write(magyar ? "Másodperc: " : "Seconds: ");
                if (int.TryParse(Console.ReadLine(), out int ido))
                {
                    hatralevIdo = ido;
                    stopwatch = Stopwatch.StartNew();
                }
            }
            else
            {
                hatralevIdo = -1;
            }

            Console.Write(magyar ? "Fedett mód? (1-Igen/2-Nem): " : "Fog of war? (1-Yes/2-No): ");
            fedettMod = Console.ReadLine() == "1";
            if (fedettMod) latogatott = new bool[terkep.GetLength(0), terkep.GetLength(1)];

            JatekosStart();
            if (fedettMod) latogatott[jatekosX, jatekosY] = true;
            Jatek();
        }

        static void TerkepBetolt(string fajl)
        {
            var sorok = File.ReadAllLines(fajl);
            terkep = new char[sorok.Length, sorok.Max(s => s.Length)];
            felfedezettTermek.Clear();

            for (int i = 0; i < sorok.Length; i++)
                for (int j = 0; j < terkep.GetLength(1); j++)
                    terkep[i, j] = j < sorok[i].Length ? sorok[i][j] : Kitolto;
        }

        static void JatekosStart()
        {
            int h = terkep.GetLength(0), w = terkep.GetLength(1);
            for (int j = 0; j < w; j++) if (JaratE(terkep[0, j])) { jatekosX = 0; jatekosY = j; return; }
            for (int i = 0; i < h; i++) if (JaratE(terkep[i, 0])) { jatekosX = i; jatekosY = 0; return; }
            for (int j = 0; j < w; j++) if (JaratE(terkep[h - 1, j])) { jatekosX = h - 1; jatekosY = j; return; }
            for (int i = 0; i < h; i++) if (JaratE(terkep[i, w - 1])) { jatekosX = i; jatekosY = w - 1; return; }
        }

        static bool JaratE(char c) => JaratKarakterek.Contains(c);

        static void Jatek()
        {
            int osszesTerem = GetRoomNumber(terkep);

            while (true)
            {
                if (hatralevIdo > 0 && hatralevIdo - (int)stopwatch.Elapsed.TotalSeconds <= 0)
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(magyar ? "\n  ⏰ VESZTETTÉL!" : "\n  ⏰ YOU LOST!");
                    Console.ResetColor();
                    Console.ReadKey();
                    return;
                }

                Console.Clear();
                Megjelenit();

                Console.WriteLine(new string('═', 119));
                string ido = hatralevIdo > 0 ? $"{hatralevIdo - (int)stopwatch.Elapsed.TotalSeconds}s" : "-";
                Console.WriteLine($"  Pozíció: [{jatekosX},{jatekosY}] | Termek: {felfedezettTermek.Count}/{osszesTerem} | Idő: {ido}");

                if (terkep[jatekosX, jatekosY] == Terem && felfedezettTermek.Add($"{jatekosX}:{jatekosY}"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n  ★ TEREM! ({felfedezettTermek.Count}/{osszesTerem})");
                    Console.ResetColor();
                    Console.ReadKey(true);
                    continue;
                }

                Console.Write("\n  ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Irányok: ");
                var iranyok = new List<string>();
                if (MehetE(jatekosX - 1, jatekosY, 'N')) iranyok.Add("↑Fel");
                if (MehetE(jatekosX, jatekosY - 1, 'W')) iranyok.Add("←Bal");
                if (MehetE(jatekosX + 1, jatekosY, 'S')) iranyok.Add("↓Le");
                if (MehetE(jatekosX, jatekosY + 1, 'E')) iranyok.Add("→Jobb");
                Console.WriteLine(iranyok.Count > 0 ? string.Join(" ", iranyok) : "-");
                Console.ResetColor();

                Console.WriteLine("\n  [WASD] mozgás | [M] Mentés | [Q] Kilépés");

                var bill = Console.ReadKey(true);
                if (bill.Key == ConsoleKey.Q)
                {
                    Console.Write(magyar ? "\n  Kilépés? (I/N): " : "\n  Quit? (Y/N): ");
                    Console.CursorVisible = true;
                    var valasz = Console.ReadLine()?.ToUpper();
                    Console.CursorVisible = false;
                    if (valasz == "I" || valasz == "Y") return;
                }
                else if (bill.Key == ConsoleKey.M) Mentes();
                else Mozgas(bill.Key, osszesTerem);
            }
        }

        static void Megjelenit()
        {
            Console.WriteLine("  " + terkepNev + "\n");
            for (int i = 0; i < terkep.GetLength(0); i++)
            {
                Console.Write("  ");
                for (int j = 0; j < terkep.GetLength(1); j++)
                {
                    if (i == jatekosX && j == jatekosY)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkGreen;
                        Console.Write(JatekosJel);
                        Console.ResetColor();
                    }
                    else if (fedettMod && !latogatott[i, j])
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write('░');
                        Console.ResetColor();
                    }
                    else
                    {
                        char c = terkep[i, j];
                        if (c == Terem)
                        {
                            Console.ForegroundColor = felfedezettTermek.Contains($"{i}:{j}") ? ConsoleColor.Yellow : ConsoleColor.Red;
                            Console.Write(felfedezettTermek.Contains($"{i}:{j}") ? '◙' : Terem);
                        }
                        else if (JaratE(c))
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(c);
                        }
                        else Console.Write(c);
                        Console.ResetColor();
                    }
                }
                Console.WriteLine();
            }
        }

        static bool MehetE(int x, int y, char ir)
        {
            if (x < 0 || y < 0 || x >= terkep.GetLength(0) || y >= terkep.GetLength(1)) return false;
            char mostani = terkep[jatekosX, jatekosY], cel = terkep[x, y];
            if (!JaratE(cel) && cel != Terem) return false;
            if (!IranyOk(mostani, ir)) return false;
            char ellentetes = ir == 'N' ? 'S' : ir == 'S' ? 'N' : ir == 'W' ? 'E' : 'W';
            return IranyOk(cel, ellentetes);
        }

        static bool IranyOk(char kar, char ir)
        {
            if (kar == Terem) return true;
            return kar switch
            {
                '╬' => true,
                '═' => ir == 'W' || ir == 'E',
                '║' => ir == 'N' || ir == 'S',
                '╦' => ir != 'N',
                '╩' => ir != 'S',
                '╣' => ir != 'E',
                '╠' => ir != 'W',
                '╗' => ir == 'W' || ir == 'S',
                '╝' => ir == 'W' || ir == 'N',
                '╚' => ir == 'E' || ir == 'N',
                '╔' => ir == 'E' || ir == 'S',
                _ => false
            };
        }

        static void Mozgas(ConsoleKey bill, int osszesTerem)
        {
            int x = jatekosX, y = jatekosY;
            char ir = '\0';

            switch (bill)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    x--; ir = 'N'; break;
                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    y--; ir = 'W'; break;
                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    x++; ir = 'S'; break;
                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    y++; ir = 'E'; break;
            }

            if (ir == '\0') return;

            if (x < 0 || y < 0 || x >= terkep.GetLength(0) || y >= terkep.GetLength(1))
            {
                if (IranyOk(terkep[jatekosX, jatekosY], ir))
                {
                    Console.WriteLine();
                    if (felfedezettTermek.Count < osszesTerem)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.CursorVisible = true;
                        Console.Write(magyar ? $"  ⚠ Még {osszesTerem - felfedezettTermek.Count} terem! Kilépsz? (I/N): " :
                            $"  ⚠ {osszesTerem - felfedezettTermek.Count} left! Exit? (Y/N): ");
                        var valasz = Console.ReadLine()?.ToUpper();
                        Console.CursorVisible = false;
                        Console.ResetColor();
                        if (valasz != "I" && valasz != "Y") return;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(magyar ? "\n  Nem teljesítve!" : "\n  Not completed!");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(magyar ? "\n  🎉 NYERTÉL!" : "\n  🎉 YOU WON!");
                    }
                    Console.ResetColor();
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                return;
            }

            if (MehetE(x, y, ir))
            {
                jatekosX = x;
                jatekosY = y;
                if (fedettMod) latogatott[jatekosX, jatekosY] = true;
            }
        }

        static void Mentes()
        {
            try
            {
                using (var w = new StreamWriter(terkepNev + ".sav"))
                {
                    // Teljes útvonal mentése
                    w.WriteLine(terkepTeljesUtvonal);
                    w.WriteLine(fedettMod);
                    w.WriteLine(jatekosX);
                    w.WriteLine(jatekosY);
                    w.WriteLine(string.Join(";", felfedezettTermek));
                    w.WriteLine(hatralevIdo);
                    if (hatralevIdo > 0) w.WriteLine((int)stopwatch.Elapsed.TotalSeconds);
                    if (fedettMod)
                        for (int i = 0; i < latogatott.GetLength(0); i++)
                            for (int j = 0; j < latogatott.GetLength(1); j++)
                                if (latogatott[i, j]) w.WriteLine($"{i}:{j}");
                }
                Console.WriteLine(magyar ? "\n  ✓ Mentve!" : "\n  ✓ Saved!");
                Console.ReadKey(true);
            }
            catch { }
        }

        static void Betoltes()
        {
            using (OpenFileDialog dialog = new OpenFileDialog 
            { 
                Filter = "Mentés fájlok|*.sav",
                InitialDirectory = Environment.CurrentDirectory 
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                try
                {
                    var s = File.ReadAllLines(dialog.FileName);
                    
                    // Térkép teljes útvonal beolvasása
                    terkepTeljesUtvonal = s[0];
                    
                    if (!File.Exists(terkepTeljesUtvonal))
                    {
                        Console.WriteLine(magyar ? $"\nHiba: Nem található a térkép fájl!\n{terkepTeljesUtvonal}" : 
                            $"\nError: Map file not found!\n{terkepTeljesUtvonal}");
                        Console.ReadKey();
                        return;
                    }

                    // Térkép betöltése
                    TerkepBetolt(terkepTeljesUtvonal);
                    terkepNev = Path.GetFileNameWithoutExtension(terkepTeljesUtvonal);
                    
                    // Játék állapot visszaállítása
                    fedettMod = bool.Parse(s[1]);
                    jatekosX = int.Parse(s[2]);
                    jatekosY = int.Parse(s[3]);
                    felfedezettTermek = new HashSet<string>(s[4].Split(';').Where(x => !string.IsNullOrEmpty(x)));
                    
                    // Idő kezelése
                    int eredetiIdo = int.Parse(s[5]);
                    hatralevIdo = eredetiIdo;
                    
                    if (hatralevIdo > 0 && s.Length > 6)
                    {
                        int elteltIdo = int.Parse(s[6]);
                        hatralevIdo = eredetiIdo - elteltIdo;
                        stopwatch = Stopwatch.StartNew();
                    }
                    
                    // Látogatott mezők visszaállítása
                    if (fedettMod)
                    {
                        latogatott = new bool[terkep.GetLength(0), terkep.GetLength(1)];
                        int startLine = hatralevIdo > 0 ? 7 : 6;
                        for (int i = startLine; i < s.Length; i++)
                        {
                            var r = s[i].Split(':');
                            if (r.Length == 2 && int.TryParse(r[0], out int row) && int.TryParse(r[1], out int col))
                                latogatott[row, col] = true;
                        }
                    }
                    
                    Console.WriteLine("\n✓ Betöltve!");
                    Console.ReadKey();
                    Jatek();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(magyar ? $"\nHiba: {ex.Message}" : $"\nError: {ex.Message}");
                    Console.ReadKey();
                }
            }
        }

        static int GetRoomNumber(char[,] map)
        {
            int db = 0;
            for (int i = 0; i < map.GetLength(0); i++)
                for (int j = 0; j < map.GetLength(1); j++)
                    if (map[i, j] == Terem) db++;
            return db;
        }

        static int GetSuitableEntrance(char[,] map)
        {
            int db = 0, h = map.GetLength(0), w = map.GetLength(1);
            for (int j = 0; j < w; j++) { if (JaratE(map[0, j])) db++; if (JaratE(map[h - 1, j])) db++; }
            for (int i = 1; i < h - 1; i++) { if (JaratE(map[i, 0])) db++; if (JaratE(map[i, w - 1])) db++; }
            return db;
        }

        static bool IsInvalidElement(char[,] map)
        {
            for (int i = 0; i < map.GetLength(0); i++)
                for (int j = 0; j < map.GetLength(1); j++)
                    if (map[i, j] != Kitolto && map[i, j] != Terem && !JaratE(map[i, j])) return true;
            return false;
        }

        static List<string> GetUnavailableElements(char[,] map)
        {
            var lista = new List<string>();
            for (int i = 0; i < map.GetLength(0); i++)
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    char k = map[i, j];
                    if (JaratE(k))
                    {
                        bool ok = false;
                        if (i > 0 && IranyOk(k, 'N') && (JaratE(map[i - 1, j]) || map[i - 1, j] == Terem) && IranyOk(map[i - 1, j], 'S')) ok = true;
                        if (i < map.GetLength(0) - 1 && IranyOk(k, 'S') && (JaratE(map[i + 1, j]) || map[i + 1, j] == Terem) && IranyOk(map[i + 1, j], 'N')) ok = true;
                        if (j > 0 && IranyOk(k, 'W') && (JaratE(map[i, j - 1]) || map[i, j - 1] == Terem) && IranyOk(map[i, j - 1], 'E')) ok = true;
                        if (j < map.GetLength(1) - 1 && IranyOk(k, 'E') && (JaratE(map[i, j + 1]) || map[i, j + 1] == Terem) && IranyOk(map[i, j + 1], 'W')) ok = true;
                        if (!ok) lista.Add($"{i}:{j}");
                    }
                }
            return lista;
        }
    }
}