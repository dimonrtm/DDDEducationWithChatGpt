class RegularizationDemo
{
    // Данные: A (3x2), b (3)
    static readonly double[,] A = { { 1, 0 }, { 0, 1 }, { 1, 1 } };
    static readonly double[] b = { 1, 2, 2 };

    static double[] ATx(double[] v) // A^T v, v size=3 -> res size=2
    {
        return new double[] {
            A[0,0]*v[0] + A[1,0]*v[1] + A[2,0]*v[2],
            A[0,1]*v[0] + A[1,1]*v[1] + A[2,1]*v[2]
        };
    }

    static double[] Ax(double[] x) // A x, x size=2 -> res size=3
    {
        return new double[] {
            A[0,0]*x[0] + A[0,1]*x[1],
            A[1,0]*x[0] + A[1,1]*x[1],
            A[2,0]*x[0] + A[2,1]*x[1]
        };
    }

    static double[] Sub(double[] u, double[] v) { var r = new double[u.Length]; for (int i = 0; i < u.Length; i++) r[i] = u[i] - v[i]; return r; }
    static double[] Add(double[] u, double[] v) { var r = new double[u.Length]; for (int i = 0; i < u.Length; i++) r[i] = u[i] + v[i]; return r; }
    static double[] Scale(double[] u, double a) { var r = new double[u.Length]; for (int i = 0; i < u.Length; i++) r[i] = a * u[i]; return r; }

    static double[] Soft(double[] z, double tau)
    {
        var r = new double[z.Length];
        for (int i = 0; i < z.Length; i++)
        {
            double s = Math.Abs(z[i]) - tau;
            r[i] = Math.Sign(z[i]) * Math.Max(s, 0.0);
        }
        return r;
    }

    // Ridge: (A^T A + 2λ I) x = A^T b  для 2x2
    static double[] Ridge(double lambda)
    {
        // A^T A = [[2,1],[1,2]] для выбранной матрицы A
        double a11 = 2 + 2 * lambda, a12 = 1;
        double a21 = 1, a22 = 2 + 2 * lambda;
        double det = a11 * a22 - a12 * a21;
        double inv11 = a22 / det, inv12 = -a12 / det;
        double inv21 = -a21 / det, inv22 = a11 / det;

        var ATb = ATx(b); // = [3,4]
        return new double[] {
            inv11*ATb[0] + inv12*ATb[1],
            inv21*ATb[0] + inv22*ATb[1]
        };
    }

    static double Linf(double[] v) { double m = 0; foreach (var t in v) m = Math.Max(m, Math.Abs(t)); return m; }

    public static void Main()
    {
        // Один шаг ISTA для LASSO
        double lambda = 0.5;
        // L = ||A||_2^2: для A, у которого A^T A имеет max eig=3, значит L=3
        double alpha = 1.0 / 3.0;
        var x0 = new double[] { 0.0, 0.0 };
        var grad0 = ATx(Sub(Ax(x0), b));     // A^T(Ax0 - b) = -A^T b
        var z = Sub(x0, Scale(grad0, alpha));
        var x1 = Soft(z, alpha * lambda);

        Console.WriteLine($"ISTA: x1 = [{x1[0]:F6}, {x1[1]:F6}]");

        // Ridge-решение
        var xr = Ridge(lambda);
        Console.WriteLine($"Ridge: x = [{xr[0]:F6}, {xr[1]:F6}]");

        // Chebyshev: ||A x - b||_inf для x=(1,1)
        var xc = new double[] { 1.0, 1.0 };
        var r = Sub(Ax(xc), b);
        Console.WriteLine($"Chebyshev residual Linf = {Linf(r):F6}");
    }
}