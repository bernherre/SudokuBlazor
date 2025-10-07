using System;
using System.Collections.Generic;
using System.Linq;

namespace Sudoku.Web.Services
{
    public class SudokuService
    {
        // Resultado de validación (anidado para evitar top-level members)
        public readonly record struct ValidationResult(bool IsValid, bool IsComplete, List<(int r, int c)> Conflicts);

        // -------------------- API pública --------------------

        public (int[,] puzzle, bool[,] fixedCells, int[,] solution) GenerateBoard(int size, string difficulty)
        {
            var (br, bc) = GetBoxDims(size);
            var solved = GenerateSolved(size, br, bc);
            ShuffleBoardInPlace(solved, br, bc);

            var puzzle = (int[,])solved.Clone();
            int blanks = GetBlanks(size, difficulty);
            RemoveCells(puzzle, blanks);

            var fixedCells = new bool[size, size];
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    fixedCells[r, c] = puzzle[r, c] != 0;

            return (puzzle, fixedCells, solved); // <- añade solved
        }


        public ValidationResult ValidateDetailed(int[,] board)
        {
            int n = board.GetLength(0);
            var (br, bc) = GetBoxDims(n);
            var conflicts = new List<(int r, int c)>();

            static void AddDupes(IEnumerable<(int r, int c, int v)> cells, List<(int r, int c)> outList)
            {
                var byVal = new Dictionary<int, List<(int r, int c)>>();
                foreach (var cell in cells)
                {
                    if (cell.v <= 0) continue; // ignorar vacías
                    if (!byVal.TryGetValue(cell.v, out var list)) byVal[cell.v] = list = new();
                    list.Add((cell.r, cell.c));
                }
                foreach (var kv in byVal)
                    if (kv.Value.Count > 1)
                        outList.AddRange(kv.Value);
            }

            // filas
            for (int r = 0; r < n; r++)
                AddDupes(Enumerable.Range(0, n).Select(c => (r, c, board[r, c])), conflicts);

            // columnas
            for (int c = 0; c < n; c++)
                AddDupes(Enumerable.Range(0, n).Select(r => (r, c, board[r, c])), conflicts);

            // cajas
            for (int r0 = 0; r0 < n; r0 += br)
            {
                for (int c0 = 0; c0 < n; c0 += bc)
                {
                    var box = new List<(int r, int c, int v)>(n);
                    for (int r = 0; r < br; r++)
                        for (int c = 0; c < bc; c++)
                            box.Add((r0 + r, c0 + c, board[r0 + r, c0 + c]));
                    AddDupes(box, conflicts);
                }
            }

            bool anyZero = false;
            for (int r = 0; r < n && !anyZero; r++)
                for (int c = 0; c < n && !anyZero; c++)
                    if (board[r, c] == 0) anyZero = true;

            bool isValid = conflicts.Count == 0;
            bool isComplete = !anyZero && isValid;
            return new ValidationResult(isValid, isComplete, conflicts);
        }

        public bool Validate(int[,] board) => ValidateDetailed(board).IsValid;

        // -------------------- Internos / helpers --------------------

        private static (int boxRows, int boxCols) GetBoxDims(int size) => size switch
        {
            4 => (2, 2),
            6 => (2, 3), // bloques 2x3
            9 => (3, 3),
            _ => throw new ArgumentException("Tamaños soportados: 4, 6, 9.")
        };

        // Patrón base válido para filas/columnas/cajas:
        // pattern(r,c) = (r*bc + r/br + c) mod n + 1
        private static int[,] GenerateSolved(int n, int br, int bc)
        {
            var g = new int[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    g[r, c] = ((r * bc) + (r / br) + c) % n + 1;
            return g;
        }

        private readonly Random _rnd = new();

        private void ShuffleBoardInPlace(int[,] g, int br, int bc)
        {
            int n = g.GetLength(0);

            // Permutar símbolos 1..n
            var perm = Enumerable.Range(1, n).OrderBy(_ => _rnd.Next()).ToArray();
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    g[r, c] = perm[g[r, c] - 1];

            // Filas dentro de cada banda
            for (int band = 0; band < n; band += br)
                ShuffleRows(g, band, br);

            // Columnas dentro de cada pila
            for (int stack = 0; stack < n; stack += bc)
                ShuffleCols(g, stack, bc);

            // Reordenar bandas completas
            ShuffleBandBlocks(g, br);

            // Reordenar pilas completas
            ShuffleStackBlocks(g, bc);
        }

        private void ShuffleRows(int[,] g, int startRow, int count)
        {
            int n = g.GetLength(0);
            var order = Enumerable.Range(0, count).OrderBy(_ => _rnd.Next()).ToArray();
            var copy = (int[,])g.Clone();
            for (int i = 0; i < count; i++)
            {
                int from = startRow + order[i];
                int to = startRow + i;
                for (int c = 0; c < n; c++)
                    g[to, c] = copy[from, c];
            }
        }

        private void ShuffleCols(int[,] g, int startCol, int count)
        {
            int n = g.GetLength(0);
            var order = Enumerable.Range(0, count).OrderBy(_ => _rnd.Next()).ToArray();
            var copy = (int[,])g.Clone();
            for (int j = 0; j < count; j++)
            {
                int from = startCol + order[j];
                int to = startCol + j;
                for (int r = 0; r < n; r++)
                    g[r, to] = copy[r, from];
            }
        }

        private void ShuffleBandBlocks(int[,] g, int br)
        {
            int n = g.GetLength(0);
            int bands = n / br;
            var order = Enumerable.Range(0, bands).OrderBy(_ => _rnd.Next()).ToArray();
            var copy = (int[,])g.Clone();

            for (int b = 0; b < bands; b++)
            {
                int fromBand = order[b];
                for (int r = 0; r < br; r++)
                {
                    int fromRow = fromBand * br + r;
                    int toRow = b * br + r;
                    for (int c = 0; c < n; c++)
                        g[toRow, c] = copy[fromRow, c];
                }
            }
        }

        private void ShuffleStackBlocks(int[,] g, int bc)
        {
            int n = g.GetLength(0);
            int stacks = n / bc;
            var order = Enumerable.Range(0, stacks).OrderBy(_ => _rnd.Next()).ToArray();
            var copy = (int[,])g.Clone();

            for (int s = 0; s < stacks; s++)
            {
                int fromStack = order[s];
                for (int c = 0; c < bc; c++)
                {
                    int fromCol = fromStack * bc + c;
                    int toCol = s * bc + c;
                    for (int r = 0; r < n; r++)
                        g[r, toCol] = copy[r, fromCol];
                }
            }
        }

        private static int GetBlanks(int size, string difficulty) => difficulty.ToLowerInvariant() switch
        {
            "easy" => (int)(size * size * 0.40),
            "medium" => (int)(size * size * 0.55),
            "hard" => (int)(size * size * 0.70),
            _ => (int)(size * size * 0.40)
        };

        private void RemoveCells(int[,] g, int blanks)
        {
            int n = g.GetLength(0);
            var positions = Enumerable.Range(0, n * n).OrderBy(_ => _rnd.Next()).ToList();
            int removed = 0;
            foreach (var p in positions)
            {
                if (removed >= blanks) break;
                int r = p / n;
                int c = p % n;
                if (g[r, c] != 0)
                {
                    g[r, c] = 0;
                    removed++;
                }
            }
        }

        private static bool NoDuplicates(IEnumerable<int> values)
        {
            var seen = new HashSet<int>();
            foreach (var v in values)
            {
                if (v == 0) continue;
                if (!seen.Add(v)) return false;
            }
            return true;
        }
    }
}
