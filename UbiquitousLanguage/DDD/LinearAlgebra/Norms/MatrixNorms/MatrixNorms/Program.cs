using System;
using System.Linq;

public static class MatrixNorms
{
    static double Norm1(double[,] A)
    {
        int m = A.GetLength(0), n = A.GetLength(1);
        double best = 0;
        for (int j = 0; j < n; j++)
        {
            double s = 0; for (int i = 0; i < m; i++) s += Math.Abs(A[i, j]);
            if (s > best) best = s;
        }
        return best;
    }

    static double NormInf(double[,] A)
    {
        int m = A.GetLength(0), n = A.GetLength(1);
        double best = 0;
        for (int i = 0; i < m; i++)
        {
            double s = 0; for (int j = 0; j < n; j++) s += Math.Abs(A[i, j]);
            if (s > best) best = s;
        }
        return best;
    }

    // Приблизим ||A||2 через спектральную норму: power iteration на B=A^T A
    static double ApproxSpectral2(double[,] A, int iters = 100)
    {
        int m = A.GetLength(0), n = A.GetLength(1);
        double[] x = new double[n];
        var rnd = new Random(1);
        for (int j = 0; j < n; j++) x[j] = rnd.NextDouble() - 0.5;
        Normalize(x);
        for (int k = 0; k < iters; k++)
        {
            var y = MulAtA(A, x); // y = (A^T A) x
            Normalize(y);
            x = y;
        }
        // рейлиевское отношение
        var Ax = Mul(A, x);
        var AtAx = MulT(A, Ax);
        double num = Dot(x, AtAx);
        return Math.Sqrt(num);
    }

    static double[] Mul(double[,] A, double[] v)
    {
        int m = A.GetLength(0), n = A.GetLength(1);
        if (v.Length != n) throw new ArgumentException("Size mismatch");
        double[] r = new double[m];
        for (int i = 0; i < m; i++)
        {
            double s = 0; for (int j = 0; j < n; j++) s += A[i, j] * v[j]; r[i] = s;
        }
        return r;
    }
    static double[] MulT(double[,] A, double[] v) // A^T v
    {
        int m = A.GetLength(0), n = A.GetLength(1);
        if (v.Length != m) throw new ArgumentException("Size mismatch");
        double[] r = new double[n];
        for (int j = 0; j < n; j++)
        {
            double s = 0; for (int i = 0; i < m; i++) s += A[i, j] * v[i]; r[j] = s;
        }
        return r;
    }
    static double[] MulAtA(double[,] A, double[] x) => MulT(A, Mul(A, x));
    static void Normalize(double[] v) { double n = Math.Sqrt(v.Sum(t => t * t)); for (int i = 0; i < v.Length; i++) v[i] /= n; }
    static double Dot(double[] a, double[] b) { double s = 0; for (int i = 0; i < a.Length; i++) s += a[i] * b[i]; return s; }

    public static void Main()
    {
        double[,] A = { { 1, -2 }, { 3, 4 } };
        Console.WriteLine($"||A||1     = {Norm1(A)}");
        Console.WriteLine($"||A||inf   = {NormInf(A)}");
        Console.WriteLine($"upper bound sqrt(||A||1||A||inf) = {Math.Sqrt(Norm1(A) * NormInf(A))}");
        Console.WriteLine($"approx ||A||2 ~ {ApproxSpectral2(A, 40):F3}");
    }
}