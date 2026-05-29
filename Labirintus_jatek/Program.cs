using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Labirintus_jatek
{
    internal class Program
    {
        // Játék karakterek
        static readonly char[] JaratKarakterek = { '╬', '═', '╦', '╩', '║', '╣', '╠', '╗', '╝', '╚', '╔' };
        static readonly char Terem = '█';
        static readonly char Kitolto = '.';
        static readonly char JatekosJel = '@';

        // Játék állapot
        static char[,] terkep;
        static bool[,] latogatott;
        static int jatekosX, jatekosY;
        static HashSet<string> felfedezettTermek = new HashSet<string>();
        static int osszesTerem;
        static bool fedettMod = false;
        static string terkepNev;
        static bool magyar = true;

        // Konzol méret
        const int CONSOLE_WIDTH = 120;
        const int CONSOLE_HEIGHT = 40;

        [STAThread]
        static void Main(string[] args)
        {
            // Konzol beállítása
            try
            {
                Console.SetWindowSize(CONSOLE_WIDTH, CONSOLE_HEIGHT);
                Console.SetBufferSize(CONSOLE_WIDTH, CONSOLE_HEIGHT);
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
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine(magyar ? "║        LABIRINTUS JÁTÉK                ║" : "║         LABYRINTH GAME                 ║");
            Console.WriteLine("╚════════════════════════════════════════╝\n");
            Console.WriteLine(magyar ? "  1 - Térkép betöltése és játék indítása" : "  1 - Load map and start game");
            Console.WriteLine(magyar ? "  2 - Mentett játék betöltése" : "  2 - Load saved game");
            Console.WriteLine(magyar ? "  3 - Nyelv váltása (Jelenlegi: HU)" : "  3 - Change language (Current: EN)");
            Console.WriteLine(magyar ? "  4 - Kilépés" : "  4 - Exit");
            Console.Write("\n  > ");

            string valasztas = Console.ReadLine();
            switch (valasztas)
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
            Console.WriteLine(magyar ? "Válassz térképfájlt..." : "Select map file...");

            string fajl = FajlValaszto("txt");

            if (string.IsNullOrEmpty(fajl))
            {
                Console.WriteLine(magyar ? "\nNem választottál fájlt!" : "\nNo file selected!");
                Console.ReadKey();
                return;
            }

            terkepNev = Path.GetFileNameWithoutExtension(fajl);
            TerkepBetolt(fajl);

            Console.Clear();
            Console.WriteLine(magyar ? "Játékmód választása:\n" : "Select game mode:\n");
            Console.WriteLine(magyar ? "  1 - Normál (teljes térkép látható)" : "  1 - Normal (full map visible)");
            Console.WriteLine(magyar ? "  2 - Fedett térkép (csak bejárt részek látszanak)" : "  2 - Fog of war (only visited areas visible)");
            Console.Write("\n  > ");

            fedettMod = Console.ReadLine() == "2";

            if (fedettMod)
                latogatott = new bool[terkep.GetLength(0), terkep.GetLength(1)];

            JatekosStart();
            
            if (fedettMod)
                latogatott[jatekosX, jatekosY] = true;

            Jatek();
        }

        static string FajlValaszto(string kiterjesztes)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = magyar ? "Válassz fájlt" : "Select file";
                dialog.Filter = kiterjesztes == "txt"
                    ? "Térkép fájlok (*.txt)|*.txt|Minden fájl (*.*)|*.*"
                    : "Mentés fájlok (*.sav)|*.sav|Minden fájl (*.*)|*.*";
                dialog.InitialDirectory = Environment.CurrentDirectory;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }
            return null;
        }

        static void TerkepBetolt(string fajl)
        {
            string[] sorok = File.ReadAllLines(fajl);
            int h = sorok.Length;
            int w = sorok.Max(s => s.Length);

            terkep = new char[h, w];
            felfedezettTermek.Clear();
            osszesTerem = 0;

            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    terkep[i, j] = j < sorok[i].Length ? sorok[i][j] : Kitolto;
                    if (terkep[i, j] == Terem) osszesTerem++;
                }
            }
        }

        static void JatekosStart()
        {
            int h = terkep.GetLength(0);
            int w = terkep.GetLength(1);

            // Első alkalmas bejárat keresése
            for (int j = 0; j < w; j++)
                if (JaratE(terkep[0, j])) { jatekosX = 0; jatekosY = j; return; }

            for (int i = 0; i < h; i++)
                if (JaratE(terkep[i, 0])) { jatekosX = i; jatekosY = 0; return; }

            for (int j = 0; j < w; j++)
                if (JaratE(terkep[h - 1, j])) { jatekosX = h - 1; jatekosY = j; return; }

            for (int i = 0; i < h; i++)
                if (JaratE(terkep[i, w - 1])) { jatekosX = i; jatekosY = w - 1; return; }
        }

        static bool JaratE(char c) => JaratKarakterek.Contains(c);

        static void Jatek()
        {
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Megjelenit();
                
                // Információk kiírása
                Console.SetCursorPosition(0, terkep.GetLength(0) + 2);
                Console.WriteLine(new string('═', CONSOLE_WIDTH - 1));
                
                string pozicio = magyar ? $"Pozíció: [{jatekosX},{jatekosY}]" : $"Position: [{jatekosX},{jatekosY}]";
                string termek = magyar ? $"Termek: {felfedezettTermek.Count}/{osszesTerem}" : $"Rooms: {felfedezettTermek.Count}/{osszesTerem}";
                Console.WriteLine($"  {pozicio}  |  {termek}");

                // Terem ellenőrzés
                if (terkep[jatekosX, jatekosY] == Terem)
                {
                    string poz = $"{jatekosX}:{jatekosY}";
                    if (!felfedezettTermek.Contains(poz))
                    {
                        felfedezettTermek.Add(poz);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(magyar ? $"\n  ★ KINCSES TERMET TALÁLTÁL! ({felfedezettTermek.Count}/{osszesTerem})" :
                            $"\n  ★ TREASURE ROOM FOUND! ({felfedezettTermek.Count}/{osszesTerem})");
                        Console.ResetColor();
                        Console.WriteLine(magyar ? "\n  Nyomj meg egy billentyűt..." : "\n  Press any key...");
                        Console.ReadKey(true);
                    }
                }

                // Győzelem ellenőrzés
                if (felfedezettTermek.Count == osszesTerem && SzelenVan())
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(magyar ? "\n  ╔═══════════════════════════════╗" : "\n  ╔═══════════════════════════════╗");
                    Console.WriteLine(magyar ? "  ║    🎉 GRATULÁLOK! 🎉          ║" : "  ║  🎉 CONGRATULATIONS! 🎉       ║");
                    Console.WriteLine(magyar ? "  ║  Minden termet felfdeztél!    ║" : "  ║   You found all rooms!        ║");
                    Console.WriteLine("  ╚═══════════════════════════════╝");
                    Console.ResetColor();
                    Console.ReadKey();
                    return;
                }

                Iranyok();
                Console.WriteLine();
                Console.WriteLine(magyar ? "  Vezérlés: [W]Fel [A]Bal [S]Le [D]Jobb | [M]Mentés [Q]Kilépés" :
                    "  Controls: [W]Up [A]Left [S]Down [D]Right | [M]Save [Q]Quit");

                ConsoleKeyInfo bill = Console.ReadKey(true);
                
                if (bill.Key == ConsoleKey.Q)
                {
                    Console.WriteLine();
                    Console.Write(magyar ? "\n  Biztosan kilépsz? (I/N): " : "\n  Are you sure to quit? (Y/N): ");
                    if (Console.ReadKey(true).Key == ConsoleKey.I || Console.ReadKey(true).Key == ConsoleKey.Y)
                        return;
                }
                else if (bill.Key == ConsoleKey.M)
                {
                    Mentes();
                }
                else
                {
                    Mozgas(bill.Key);
                }
            }
        }

        static void Megjelenit()
        {
            // Térkép középre igazítása
            int startX = 5;
            int startY = 2;

            Console.SetCursorPosition(0, 0);
            Console.WriteLine(magyar ? "  LABIRINTUS - " + terkepNev : "  LABYRINTH - " + terkepNev);
            Console.WriteLine();

            for (int i = 0; i < terkep.GetLength(0); i++)
            {
                Console.SetCursorPosition(startX, startY + i);
                for (int j = 0; j < terkep.GetLength(1); j++)
                {
                    if (i == jatekosX && j == jatekosY)
                    {
                        // Játékos megjelenítése háttérrel
                        Console.BackgroundColor = ConsoleColor.DarkGreen;
                        Console.ForegroundColor = ConsoleColor.White;
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
                            string poz = $"{i}:{j}";
                            if (felfedezettTermek.Contains(poz))
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write('◙'); // Felfedezett terem
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write(Terem);
                            }
                        }
                        else if (JaratE(c))
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(c);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write(c);
                        }
                        Console.ResetColor();
                    }
                }
            }
        }

        static void Iranyok()
        {
            List<string> ir = new List<string>();
            if (MehetE(jatekosX - 1, jatekosY, 'N')) ir.Add(magyar ? "↑Fel" : "↑Up");
            if (MehetE(jatekosX, jatekosY - 1, 'W')) ir.Add(magyar ? "←Bal" : "←Left");
            if (MehetE(jatekosX + 1, jatekosY, 'S')) ir.Add(magyar ? "↓Le" : "↓Down");
            if (MehetE(jatekosX, jatekosY + 1, 'E')) ir.Add(magyar ? "→Jobb" : "→Right");

            if (ir.Count > 0)
            {
                Console.WriteLine();
                Console.Write(magyar ? "  Lehetséges irányok: " : "  Available directions: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(string.Join(" | ", ir));
                Console.ResetColor();
            }
        }

        static bool MehetE(int x, int y, char ir)
        {
            if (x < 0 || y < 0 || x >= terkep.GetLength(0) || y >= terkep.GetLength(1))
                return false;

            char mostani = terkep[jatekosX, jatekosY];
            char cel = terkep[x, y];

            if (!JaratE(cel) && cel != Terem) return false;
            if (!IranyOk(mostani, ir)) return false;

            char ellentetes = ir == 'N' ? 'S' : ir == 'S' ? 'N' : ir == 'W' ? 'E' : 'W';
            return IranyOk(cel, ellentetes);
        }

        static bool IranyOk(char kar, char ir)
        {
            if (kar == Terem) return true;

            switch (kar)
            {
                case '╬': return true;
                case '═': return ir == 'W' || ir == 'E';
                case '║': return ir == 'N' || ir == 'S';
                case '╦': return ir == 'W' || ir == 'E' || ir == 'S';
                case '╩': return ir == 'W' || ir == 'E' || ir == 'N';
                case '╣': return ir == 'W' || ir == 'N' || ir == 'S';
                case '╠': return ir == 'E' || ir == 'N' || ir == 'S';
                case '╗': return ir == 'W' || ir == 'S';
                case '╝': return ir == 'W' || ir == 'N';
                case '╚': return ir == 'E' || ir == 'N';
                case '╔': return ir == 'E' || ir == 'S';
                default: return false;
            }
        }

        static void Mozgas(ConsoleKey bill)
        {
            int x = jatekosX, y = jatekosY;
            char ir = ' ';

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
                default: return;
            }

            // Kijutás ellenőrzés
            if (x < 0 || y < 0 || x >= terkep.GetLength(0) || y >= terkep.GetLength(1))
            {
                if (IranyOk(terkep[jatekosX, jatekosY], ir))
                {
                    Console.SetCursorPosition(0, terkep.GetLength(0) + 8);
                    
                    if (felfedezettTermek.Count < osszesTerem)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(magyar ? 
                            $"\n  ⚠ Figyelem! Még {osszesTerem - felfedezettTermek.Count} terem van hátra!" :
                            $"\n  ⚠ Warning! Still {osszesTerem - felfedezettTermek.Count} rooms left!");
                        Console.ResetColor();
                        Console.Write(magyar ? "  Biztosan kilépsz? (I/N): " : "  Exit anyway? (Y/N): ");
                        
                        ConsoleKeyInfo valasz = Console.ReadKey(true);
                        if (valasz.Key == ConsoleKey.I || valasz.Key == ConsoleKey.Y)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(magyar ? "\n\n  Kijutottál, de nem teljesítetted a célt!" : 
                                "\n\n  You escaped, but didn't complete the objective!");
                            Console.ResetColor();
                            Console.ReadKey();
                        }
                        return;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(magyar ? "\n  🎉 GRATULÁLOK! Kijutottál!" : "\n  🎉 CONGRATULATIONS! You escaped!");
                        Console.ResetColor();
                        Console.ReadKey();
                    }
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

        static bool SzelenVan()
        {
            bool szel = jatekosX == 0 || jatekosY == 0 ||
                       jatekosX == terkep.GetLength(0) - 1 ||
                       jatekosY == terkep.GetLength(1) - 1;
            if (!szel) return false;

            char k = terkep[jatekosX, jatekosY];
            if (jatekosX == 0 && IranyOk(k, 'N')) return true;
            if (jatekosY == 0 && IranyOk(k, 'W')) return true;
            if (jatekosX == terkep.GetLength(0) - 1 && IranyOk(k, 'S')) return true;
            if (jatekosY == terkep.GetLength(1) - 1 && IranyOk(k, 'E')) return true;
            return false;
        }

        static void Mentes()
        {
            try
            {
                string fajl = terkepNev + ".sav";
                using (StreamWriter w = new StreamWriter(fajl))
                {
                    w.WriteLine(terkepNev + ".txt");
                    w.WriteLine(fedettMod);
                    w.WriteLine(jatekosX);
                    w.WriteLine(jatekosY);
                    w.WriteLine(string.Join(";", felfedezettTermek));

                    if (fedettMod)
                    {
                        for (int i = 0; i < latogatott.GetLength(0); i++)
                            for (int j = 0; j < latogatott.GetLength(1); j++)
                                if (latogatott[i, j]) w.WriteLine($"{i}:{j}");
                    }
                }
                
                Console.SetCursorPosition(0, terkep.GetLength(0) + 8);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(magyar ? $"\n  ✓ Játék mentve: {fajl}" : $"\n  ✓ Game saved: {fajl}");
                Console.ResetColor();
                Console.WriteLine(magyar ? "  Nyomj meg egy billentyűt..." : "  Press any key...");
                Console.ReadKey(true);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n  Hiba: {ex.Message}");
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        static void Betoltes()
        {
            Console.Clear();
            Console.WriteLine(magyar ? "Válassz mentésfájlt..." : "Select save file...");

            string fajl = FajlValaszto("sav");

            if (string.IsNullOrEmpty(fajl))
            {
                Console.WriteLine(magyar ? "\nNem választottál fájlt!" : "\nNo file selected!");
                Console.ReadKey();
                return;
            }

            try
            {
                string[] s = File.ReadAllLines(fajl);
                
                // Térkép fájl elérési útja
                string terkepFajl = s[0];
                if (!Path.IsPathRooted(terkepFajl))
                {
                    // Ha relatív útvonal, akkor a mentés fájl mappájához viszonyítva keressük
                    string savDir = Path.GetDirectoryName(fajl);
                    terkepFajl = Path.Combine(savDir, terkepFajl);
                }
                
                TerkepBetolt(terkepFajl);
                terkepNev = Path.GetFileNameWithoutExtension(terkepFajl);
                fedettMod = bool.Parse(s[1]);
                jatekosX = int.Parse(s[2]);
                jatekosY = int.Parse(s[3]);

                felfedezettTermek.Clear();
                if (!string.IsNullOrEmpty(s[4]))
                    foreach (string t in s[4].Split(';'))
                        felfedezettTermek.Add(t);

                if (fedettMod)
                {
                    latogatott = new bool[terkep.GetLength(0), terkep.GetLength(1)];
                    for (int i = 5; i < s.Length; i++)
                    {
                        string[] r = s[i].Split(':');
                        latogatott[int.Parse(r[0]), int.Parse(r[1])] = true;
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(magyar ? "\n✓ Játék betöltve!" : "\n✓ Game loaded!");
                Console.ResetColor();
                Console.WriteLine(magyar ? "Nyomj meg egy billentyűt..." : "Press any key...");
                Console.ReadKey();
                Jatek();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nHiba: {ex.Message}");
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        // ===== KÖTELEZŐ METÓDUSOK =====

        /// <summary>
        /// Megadja, hogy hány termet tartalmaz a térkép
        /// </summary>
        static int GetRoomNumber(char[,] map)
        {
            int db = 0;
            for (int i = 0; i < map.GetLength(0); i++)
                for (int j = 0; j < map.GetLength(1); j++)
                    if (map[i, j] == Terem) db++;
            return db;
        }

        /// <summary>
        /// A kapott térkép széleit végignézve megállapítja, hogy hány kijárat van.
        /// </summary>
        static int GetSuitableEntrance(char[,] map)
        {
            int db = 0;
            int h = map.GetLength(0);
            int w = map.GetLength(1);

            for (int j = 0; j < w; j++)
            {
                if (JaratE(map[0, j])) db++;
                if (JaratE(map[h - 1, j])) db++;
            }

            for (int i = 1; i < h - 1; i++)
            {
                if (JaratE(map[i, 0])) db++;
                if (JaratE(map[i, w - 1])) db++;
            }

            return db;
        }

        /// <summary>
        /// Megnézi, hogy van-e a térképen meg nem engedett karakter?
        /// </summary>
        static bool IsInvalidElement(char[,] map)
        {
            for (int i = 0; i < map.GetLength(0); i++)
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    char c = map[i, j];
                    if (c != Kitolto && c != Terem && !JaratE(c))
                        return true;
                }
            return false;
        }

        /// <summary>
        /// Visszaadja azoknak a járatkaraktereknek a pozícióját, amelyekhez egyetlen szomszéd pozícióból sem lehet eljutni.
        /// </summary>
        static List<string> GetUnavailableElements(char[,] map)
        {
            List<string> lista = new List<string>();

            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    char k = map[i, j];
                    if (JaratE(k))
                    {
                        bool ok = false;

                        if (i > 0 && IranyOk(k, 'N'))
                        {
                            char sz = map[i - 1, j];
                            if ((JaratE(sz) || sz == Terem) && IranyOk(sz, 'S')) ok = true;
                        }

                        if (i < map.GetLength(0) - 1 && IranyOk(k, 'S'))
                        {
                            char sz = map[i + 1, j];
                            if ((JaratE(sz) || sz == Terem) && IranyOk(sz, 'N')) ok = true;
                        }

                        if (j > 0 && IranyOk(k, 'W'))
                        {
                            char sz = map[i, j - 1];
                            if ((JaratE(sz) || sz == Terem) && IranyOk(sz, 'E')) ok = true;
                        }

                        if (j < map.GetLength(1) - 1 && IranyOk(k, 'E'))
                        {
                            char sz = map[i, j + 1];
                            if ((JaratE(sz) || sz == Terem) && IranyOk(sz, 'W')) ok = true;
                        }

                        if (!ok) lista.Add($"{i}:{j}");
                    }
                }
            }
            return lista;
        }

        /// <summary>
        /// Labirintust generál a kapott pozíciókat tartalmazó lista alapján.
        /// </summary>
        static char[,] GenerateLabyrinth(List<string> positionsList)
        {
            if (positionsList == null || positionsList.Count == 0) return null;

            int maxX = 0, maxY = 0;
            foreach (string p in positionsList)
            {
                string[] r = p.Split(':');
                int x = int.Parse(r[0]);
                int y = int.Parse(r[1]);
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }

            char[,] map = new char[maxX + 1, maxY + 1];

            for (int i = 0; i < map.GetLength(0); i++)
                for (int j = 0; j < map.GetLength(1); j++)
                    map[i, j] = Kitolto;

            foreach (string p in positionsList)
            {
                string[] r = p.Split(':');
                map[int.Parse(r[0]), int.Parse(r[1])] = '╬';
            }

            return map;
        }
    }
}