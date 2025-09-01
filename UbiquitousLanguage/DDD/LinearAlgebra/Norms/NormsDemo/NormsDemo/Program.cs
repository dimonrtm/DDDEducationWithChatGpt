using System;
using System.Linq;

public static class NormsDemo
{
    static double NormL1(double[] v) => v.Sum(t => Math.Abs(t));
    static double NormL2(double[] v) => Math.Sqrt(v.Sum(t => t * t));
    static double NormLInf(double[] v) => v.Select(Math.Abs).Max();

    static double[] Add(double[] a, double[] b)
    {
        if (a.Length != b.Length) throw new ArgumentException("Vectors must match in length.");
        var r = new double[a.Length];
        for (int i = 0; i < a.Length; i++) r[i] = a[i] + b[i];
        return r;
    }

    public static void Main()
    {
        var x = new double[] { 3, -4, 0, 1 };
        var y = new double[] { 1, 1, 1, 1 };
        var s = Add(x, y);

        Console.WriteLine($"L1(x)={NormL1(x)}, L2(x)={NormL2(x)}, L∞(x)={NormLInf(x)}");
        Console.WriteLine($"L1(y)={NormL1(y)}, L2(y)={NormL2(y)}, L∞(y)={NormLInf(y)}");
        Console.WriteLine($"L1(x+y)={NormL1(s)}, L2(x+y)={NormL2(s)}, L∞(x+y)={NormLInf(s)}");

        Console.WriteLine($"Triangle (L1): {NormL1(s)} <= {NormL1(x) + NormL1(y)}");
        Console.WriteLine($"Triangle (L2): {NormL2(s)} <= {NormL2(x) + NormL2(y)}");
        Console.WriteLine($"Triangle (L∞): {NormLInf(s)} <= {NormLInf(x) + NormLInf(y)}");
    }
}