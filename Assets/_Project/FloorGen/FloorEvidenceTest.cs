using UnityEngine;

namespace DebtDungeon.FloorGen
{
   // generates many floors without drawing them and reports whether the rules always hold 
   public static class FloorEvidenceTest
    {
        public static void Run(FloorSettings settings, int floorCount, int firstSeed)
        {
            int disconnected = 0;
            int exitInMajor = 0;
            int startNotNeutral = 0;
            int tooManyMajors = 0;
            int factionWithoutMajor = 0;

            int minExit = int.MaxValue;
            int maxExit = 0;
            long totalExit = 0;
            int exitInTarget = 0; // target from pacing estimate: 6-9 cells
            int[] exitCounts = new int[settings.gridSize * settings.gridSize];

            long neutralCells = 0;
            long goblinCells = 0;
            long spiderCells = 0;

            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i< floorCount; i++)
            {
                FloorLayoutGenerator layout = new FloorLayoutGenerator(settings, firstSeed + i);
                CellData[,] cells = layout.Generate();

                if (layout.UnreachableCount > 0) disconnected++;

                CellData start = cells[layout.StartCell.x, layout.StartCell.y];
                CellData exit = cells[layout.ExitCell.x, layout.ExitCell.y];
                if (start.Owner != Faction.Neutral) startNotNeutral++;
                if (exit.Band == Intensity.Major) exitInMajor++;

                int goblinMajors = 0;
                int spiderMajors = 0;
                foreach (CellData cell in cells)
                {
                    if (cell.Owner == Faction.Goblin)
                    {
                        goblinCells++;
                        if (cell.Band == Intensity.Major) goblinMajors++;
                    }
                    else if (cell.Owner == Faction.Spider)
                    {
                        spiderCells++;
                        if (cell.Band == Intensity.Major) spiderMajors++;
                    }
                    else
                    {
                        neutralCells++;
                    }
                }

                if (goblinMajors > settings.maxMajorPerFaction || spiderMajors > settings.maxMajorPerFaction) tooManyMajors++;
                if (goblinMajors == 0 || spiderMajors == 0) factionWithoutMajor++;

                int distance = exit.DistanceFromStart;
                totalExit += distance;
                if (distance < minExit) minExit = distance;
                if (distance > maxExit) maxExit = distance;
                if (distance >= 6 && distance <= 9) exitInTarget++;
                if (distance >= 0 && distance < exitCounts.Length) exitCounts[distance]++;
            }

            timer.Stop();

            // generating the same seed twice must give an identical floor
            int determinismFailures = 0; // pulled terminology from Google Search
            for (int i = 0; i < 100; i++)
            {
                CellData[,] first = new FloorLayoutGenerator(settings, firstSeed + i).Generate();
                CellData[,] second = new FloorLayoutGenerator(settings, firstSeed + i).Generate();
                if (!SameFloor(first, second)) determinismFailures++;
            }

            int totalCells = floorCount * settings.gridSize * settings.gridSize;
            string histogram = "";
            for (int d = 0; d < exitCounts.Length; d++)
            {
                if (exitCounts[d] > 0) histogram += d + ":" + exitCounts[d] + "  ";
            }

            Debug.Log("===== FLOOR EVIDENCE TEST =====\n" +
                "Floors: " + floorCount + " (seeds " + firstSeed + "-" + (firstSeed + floorCount - 1) + ")" +
                " | Grid " + settings.gridSize + "x" + settings.gridSize + " | Extra connections: " + settings.extraConnections + "\n" +
                "Disconnected floors: " + disconnected + "\n" +
                "Start not neutral: " + startNotNeutral + "\n" +
                "Exit placed in a Major cell: " + exitInMajor + "\n" +
                "Faction over Major cap: " + tooManyMajors + "\n" +
                "Floors where a faction has no Major: " + factionWithoutMajor + "\n" +
                "Determinism failures (100 seeds, generated twice): " + determinismFailures + "\n" +
                "Exit distance: min " + minExit + ", avg " + (totalExit / (float)floorCount).ToString("F1") + ", max " + maxExit +
                " | in 6-9 range: " + (100f * exitInTarget / floorCount).ToString("F1") + "%\n" +
                "Exit distance counts: " + histogram + "\n" +
                "Territory share: neutral " + (100f * neutralCells / totalCells).ToString("F1") + "%, goblin " +
                (100f * goblinCells / totalCells).ToString("F1") + "%, spider " + (100f * spiderCells / totalCells).ToString("F1") + "%\n" +
                "Layout time: " + timer.ElapsedMilliseconds + " ms total, " +
                (timer.ElapsedMilliseconds / (float)floorCount).ToString("F3") + " ms per floor");
        }

        private static bool SameFloor(CellData[,] a, CellData[,] b)
        {
            for (int x = 0; x < a.GetLength(0); x++)
            {
                for (int y = 0; y < a.GetLength(1); y++)
                {
                    CellData ca = a[x, y];
                    CellData cb = b[x, y];
                    if (ca.Owner != cb.Owner || ca.Band != cb.Band || ca.Openings != cb.Openings ||
                        ca.IsStart != cb.IsStart || ca.IsExit != cb.IsExit)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
    
}
