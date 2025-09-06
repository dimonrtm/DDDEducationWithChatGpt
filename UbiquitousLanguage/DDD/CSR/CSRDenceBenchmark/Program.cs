using System;
using System.Collections.Generic;
using System.Diagnostics;

public sealed class CsrMatrix
{
    public int Rows, Cols;
    public int[] RowPtr, ColIdx;
    public double[] Val;

    public CsrMatrix(int rows, int cols, int[] rowPtr, int[] colIdx, double[] val)
    {
        Rows = rows; Cols = cols;
        RowPtr = rowPtr; ColIdx = colIdx; Val = val;
    }

    // C = A * B, где A — CSR (Rows x Cols), B — плотная (Cols x k)
    public double[,] Multiply(double[,] B)
    {
        int m = Cols, k = B.GetLength(1);
        if (B.GetLength(0) != m) throw new ArgumentException("Dim mismatch A.Cols vs B.Rows");
        var C = new double[Rows, k];

        for (int i = 0; i < Rows; i++)
        {
            int start = RowPtr[i], end = RowPtr[i + 1];
            for (int p = start; p < end; p++)
            {
                int j = ColIdx[p];
                double a = Val[p];
                for (int t = 0; t < k; t++)
                    C[i, t] += a * B[j, t];
            }
        }
        return C;
    }

    public double[] Multiply1D(double[] B, int k)
    {
        int m = Cols;
        if (B.Length != m * k) throw new ArgumentException("Dim mismatch A.Cols vs B.Rows");

        var C = new double[Rows * k];

        for (int i = 0; i < Rows; i++)
        {
            int ci = i * k;
            int start = RowPtr[i], end = RowPtr[i + 1];
            for (int p = start; p < end; p++)
            {
                int j = ColIdx[p];
                double a = Val[p];
                int bj = j * k;

                int t = 0;
                // лёгкое развёртывание по 4 (обычно достаточно)
                for (; t <= k - 4; t += 4)
                {
                    C[ci + t + 0] += a * B[bj + t + 0];
                    C[ci + t + 1] += a * B[bj + t + 1];
                    C[ci + t + 2] += a * B[bj + t + 2];
                    C[ci + t + 3] += a * B[bj + t + 3];
                }
                for (; t < k; t++)
                    C[ci + t] += a * B[bj + t];
            }
        }
        return C;
    }
}

public static class Bench
{
    static readonly Random Rng = new(123);

    public static CsrMatrix GenerateRandomCsr(int n, int m, double density)
    {
        var rowPtr = new int[n + 1];
        var col = new List<int>(capacity: (int)(n * m * density) + 1);
        var val = new List<double>(capacity: (int)(n * m * density) + 1);

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
                if (Rng.NextDouble() < density)
                {
                    col.Add(j);
                    // значения не нули; масштабируем, чтобы избежать денормалов
                    val.Add(Rng.NextDouble() * 2.0 - 1.0);
                }
            rowPtr[i + 1] = col.Count;
        }
        return new CsrMatrix(n, m, rowPtr, col.ToArray(), val.ToArray());
    }

    public static double[,] GenerateDense(int m, int k)
    {
        var B = new double[m, k];
        for (int i = 0; i < m; i++)
            for (int t = 0; t < k; t++)
                B[i, t] = Rng.NextDouble() * 2.0 - 1.0;
        return B;
    }
    public static double[] GenerateDense1D(int m, int k, int seed = 123)
    {
        var rng = new Random(seed);
        var B = new double[m * k];
        for (int j = 0; j < m; j++)
            for (int t = 0; t < k; t++)
                B[j * k + t] = rng.NextDouble() * 2.0 - 1.0;
        return B;
    }

    public static (double medianMs, double checksum) TimeIt(Func<double[]> action, int reps = 10)
    {
        // прогрев
        _ = action();
        var times = new double[reps];
        double checksum = 0;
        for (int r = 0; r < reps; r++)
        {
            var sw = Stopwatch.StartNew();
            var C = action();
            sw.Stop();
            times[r] = sw.Elapsed.TotalMilliseconds;

            // маленький checksum, чтобы JIT не выкинул результат
            for (int i = 0; i < C.Length; i += Math.Max(1, C.Length / 8))
                    checksum += C[i] * 1e-12;
        }
        Array.Sort(times);
        return (times[times.Length / 2], checksum);
    }
}

class Program
{
    public static void SortColumnsInRows(CsrMatrix A)
    {
        for (int i = 0; i < A.Rows; i++)
        {
            int start = A.RowPtr[i], end = A.RowPtr[i + 1];
            int len = end - start;
            if (len <= 1) continue;

            // вынимаем подмассивы строки
            var cols = new int[len];
            var vals = new double[len];
            Array.Copy(A.ColIdx, start, cols, 0, len);
            Array.Copy(A.Val, start, vals, 0, len);

            // сортируем по столбцам с «прицепленными» значениями
            Array.Sort(cols, vals);

            // возвращаем на место
            Array.Copy(cols, 0, A.ColIdx, start, len);
            Array.Copy(vals, 0, A.Val, start, len);
        }
    }

    public static double[] ToDense1D(CsrMatrix A)
    {
        var D = new double[A.Rows * A.Cols];
        for (int i = 0; i < A.Rows; i++)
        {
            int start = A.RowPtr[i], end = A.RowPtr[i + 1];
            int baseA = i * A.Cols;
            for (int p = start; p < end; p++)
                D[baseA + A.ColIdx[p]] = A.Val[p];
        }
        return D;
    }

    public static double[] GemmRowMajor(double[] A, int n, int m, double[] B, int k)
    {
        if (A.Length != n * m || B.Length != m * k) throw new ArgumentException();
        var C = new double[n * k];
        for (int i = 0; i < n; i++)
        {
            int ai = i * m, ci = i * k;
            for (int j = 0; j < m; j++)
            {
                double a = A[ai + j];
                if (a == 0) continue; // можно оставить, но для "честной" dense уберите условие
                int bj = j * k;
                int t = 0;
                for (; t <= k - 4; t += 4)
                {
                    C[ci + t + 0] += a * B[bj + t + 0];
                    C[ci + t + 1] += a * B[bj + t + 1];
                    C[ci + t + 2] += a * B[bj + t + 2];
                    C[ci + t + 3] += a * B[bj + t + 3];
                }
                for (; t < k; t++)
                    C[ci + t] += a * B[bj + t];
            }
        }
        return C;
    }

    static void Main()
    {
        // ПАРАМЕТРЫ ЭКСПЕРИМЕНТА — меняйте под задачу
        int n = 2000, m = 2000, k = 128;
        double density = 0.01; // 0.5% ненулевых

        var A = Bench.GenerateRandomCsr(n, m, density);
        SortColumnsInRows(A); 
        //var B = Bench.GenerateDense(m, k);
        var B1 = Bench.GenerateDense1D(m, k);

        var (tCsr, chk1) = Bench.TimeIt(() => A.Multiply1D(B1, k), reps: 11);
        var A_dense = ToDense1D(A);
        var (tDense, chk2) = Bench.TimeIt(() => GemmRowMajor(A_dense, n, m, B1, k), reps: 11);

        Console.WriteLine($"CSR median = {tCsr:F1} ms, DENSE median = {tDense:F1} ms");

        /*Console.WriteLine($"A: {n}x{m}, nnz={A.Val.Length} (~{density:P1}), B: {m}x{k}");

        //var (median, chk) = Bench.TimeIt(() => A.Multiply(B), reps: 11);
        var (median, chk) = Bench.TimeIt(() => A.Multiply1D(B1, k), reps: 11);
        // Грубая оценка FLOPs: 2 * nnz * k
        double gflops = (2.0 * A.Val.Length * k) / (median / 1e3) / 1e9;

        Console.WriteLine($"CSR·dense median: {median:F1} ms | ~{gflops:F2} GF/s | checksum={chk:F3}");*/
    }
}
