namespace TheLedger.Sim.Tackle;

/// <summary>
/// Direct XPBD solver for the whole tackle chain: rod stretch + rod bending + every line segment, solved together
/// as one banded linear system per iteration (after Deul, Kugelstadt, Weiler, Bender, "Direct Position-Based Solver
/// for Stiff Rods", 2018). Iterative Gauss-Seidel makes a graded rod blank much softer than its EI and makes the
/// rod/line coupling fight itself; a direct solve gives the stiffness the data asks for, independent of substeps.
///
/// Constraint order along the chain: [bend 1, stretch 1, bend 2, stretch 2, …, bend R−2, stretch R−2, line 0 … line L−1].
/// Every constraint only touches constraints at most 4 places away, so the matrix J·W·Jᵀ + α̃ is banded with
/// half-bandwidth 4 and a banded Cholesky solves it in linear time.
///
/// Rayleigh damping (XPBD's γ = τ/h) is folded in symmetrically by scaling each row by 1/(1+γ).
/// </summary>
public sealed class TackleSolver
{
    private const int B = 4; // half-bandwidth

    private readonly int _maxParticles;
    private readonly int _maxConstraints;

    // Gathered particles: rod nodes 0..R−1, then line nodes R..R+L−1.
    private readonly double[] _x, _y, _px, _py, _w;

    // Constraints.
    private readonly byte[] _kind;          // 0 = distance, 1 = bend
    private readonly int[] _n0, _n1, _n2;
    private readonly double[] _rest, _alpha, _gamma, _lambda, _c;
    private readonly double[] _g;           // gradients: 6 per constraint (node 0, 1, 2)
    private readonly bool[] _active, _unilateral;
    private readonly int[] _ownerIndex;     // rod joint j (bend), rod segment j (stretch), or line segment s
    private int _count;

    // Band storage: row p, offset k (0..B) = entry (p, p−k).
    private readonly double[] _band;
    private readonly double[] _rhs, _dl, _tmp;

    public TackleSolver(int maxRodNodes, int maxLineNodes)
    {
        _maxParticles = maxRodNodes + maxLineNodes;
        _maxConstraints = 2 * maxRodNodes + maxLineNodes + 4;
        int p = _maxParticles, c = _maxConstraints;
        _x = new double[p]; _y = new double[p]; _px = new double[p]; _py = new double[p]; _w = new double[p];
        _kind = new byte[c]; _n0 = new int[c]; _n1 = new int[c]; _n2 = new int[c];
        _rest = new double[c]; _alpha = new double[c]; _gamma = new double[c]; _lambda = new double[c]; _c = new double[c];
        _g = new double[c * 6];
        _active = new bool[c]; _unilateral = new bool[c];
        _ownerIndex = new int[c];
        _band = new double[c * (B + 1)];
        _rhs = new double[c]; _dl = new double[c]; _tmp = new double[c];
    }

    /// <summary>Solves rod and line constraints for one substep; writes positions, rod bend λ and line λ back.</summary>
    public void Solve(Rod rod, Line line, float h, float rodGammaTime, float rodStretchCompliance, int iterations)
    {
        int R = rod.Count, L = line.Count;
        int nP = R + L;
        for (int i = 0; i < R; i++) { _x[i] = rod.X[i]; _y[i] = rod.Y[i]; _px[i] = rod.PX[i]; _py[i] = rod.PY[i]; _w[i] = rod.W[i]; }
        for (int k = 0; k < L; k++) { int i = R + k; _x[i] = line.X[k]; _y[i] = line.Y[k]; _px[i] = line.PX[k]; _py[i] = line.PY[k]; _w[i] = line.SolverW[k]; }

        double h2 = (double)h * h;
        double rodGamma = rodGammaTime / (double)h;
        double invEA = 1.0 / line.Spec.EA;

        // Build the constraint list.
        _count = 0;
        for (int j = 1; j < R - 1; j++)
        {
            Add(1, j - 1, j, j + 1, 0.0, rod.BendCompliance[j] / h2, rodGamma, false, j);
            Add(0, j, j + 1, -1, rod.RestLen[j], rodStretchCompliance / h2, rodGamma, false, j);
        }
        int lineStart = _count;
        for (int s = 0; s < L; s++)
        {
            int a = s == 0 ? R - 1 : R + s - 1;
            int b = R + s;
            double rest = s == 0 ? line.FirstRest : line.Rest[s - 1];
            Add(0, a, b, -1, rest, rest * invEA / h2, 0.0, true, s);
        }
        for (int p = 0; p < _count; p++) _lambda[p] = 0.0;

        for (int it = 0; it < Math.Max(1, iterations); it++)
        {
            Linearize(nP);
            Assemble();
            if (!Factor()) break;
            SolveBand();
            Apply();
        }

        // Diagnostics: worst remaining violation after the solve (line stretch beyond rest, rod stretch, rod bend).
        Linearize(nP);
        double worstLine = 0, worstRod = 0;
        for (int p = 0; p < _count; p++)
        {
            if (!_active[p]) continue;
            double v = Math.Abs(_c[p]);
            if (p >= lineStart) { if (_c[p] > worstLine) worstLine = _c[p]; }
            else if (_kind[p] == 0 && v > worstRod) worstRod = v;
        }
        LastLineResidual = worstLine;
        LastRodStretchResidual = worstRod;

        for (int i = 0; i < R; i++) { rod.X[i] = (float)_x[i]; rod.Y[i] = (float)_y[i]; }
        for (int k = 0; k < L; k++) { line.X[k] = (float)_x[R + k]; line.Y[k] = (float)_y[R + k]; }
        for (int p = 0; p < lineStart; p++)
            if (_kind[p] == 1) rod.BendLambda[_ownerIndex[p]] = (float)_lambda[p];
        for (int p = lineStart; p < _count; p++)
            line.Lambda[_ownerIndex[p]] = _active[p] ? (float)_lambda[p] : 0f;
    }

    /// <summary>Largest line over-stretch left after the last solve, m (diagnostic).</summary>
    public double LastLineResidual { get; private set; }
    public double LastRodStretchResidual { get; private set; }

    private void Add(byte kind, int a, int b, int c, double rest, double alphaTilde, double gamma, bool unilateral, int owner)
    {
        int p = _count++;
        _kind[p] = kind; _n0[p] = a; _n1[p] = b; _n2[p] = c;
        _rest[p] = rest; _alpha[p] = alphaTilde; _gamma[p] = gamma; _unilateral[p] = unilateral;
        _ownerIndex[p] = owner;
    }

    /// <summary>Constraint values and gradients at the current positions.</summary>
    private void Linearize(int nP)
    {
        for (int p = 0; p < _count; p++)
        {
            int g = p * 6;
            if (_kind[p] == 0)
            {
                int a = _n0[p], b = _n1[p];
                double dx = _x[b] - _x[a], dy = _y[b] - _y[a];
                double len = Math.Sqrt(dx * dx + dy * dy);
                _c[p] = len - _rest[p];
                bool usable = len > 1e-12 && (_w[a] > 0 || _w[b] > 0);
                _active[p] = usable && (!_unilateral[p] || _c[p] > 0);
                if (!_active[p]) { _g[g] = _g[g + 1] = _g[g + 2] = _g[g + 3] = _g[g + 4] = _g[g + 5] = 0; continue; }
                double nx = dx / len, ny = dy / len;
                _g[g] = -nx; _g[g + 1] = -ny; _g[g + 2] = nx; _g[g + 3] = ny; _g[g + 4] = 0; _g[g + 5] = 0;
            }
            else
            {
                int a = _n0[p], b = _n1[p], c = _n2[p];
                double d1x = _x[b] - _x[a], d1y = _y[b] - _y[a];
                double d2x = _x[c] - _x[b], d2y = _y[c] - _y[b];
                double l1 = d1x * d1x + d1y * d1y, l2 = d2x * d2x + d2y * d2y;
                bool usable = l1 > 1e-18 && l2 > 1e-18 && (_w[a] > 0 || _w[b] > 0 || _w[c] > 0);
                _active[p] = usable;
                if (!usable) { _c[p] = 0; _g[g] = _g[g + 1] = _g[g + 2] = _g[g + 3] = _g[g + 4] = _g[g + 5] = 0; continue; }
                _c[p] = Math.Atan2(d1x * d2y - d1y * d2x, d1x * d2x + d1y * d2y) - _rest[p];
                double gax = -d1y / l1, gay = d1x / l1;
                double gcx = -d2y / l2, gcy = d2x / l2;
                _g[g] = gax; _g[g + 1] = gay;
                _g[g + 2] = -gax - gcx; _g[g + 3] = -gay - gcy;
                _g[g + 4] = gcx; _g[g + 5] = gcy;
            }
        }
    }

    private double Couple(int p, int q)
    {
        // Σ over nodes shared by constraints p and q of w_i · (∇_i C_p · ∇_i C_q).
        double sum = 0;
        int np = _kind[p] == 1 ? 3 : 2, nq = _kind[q] == 1 ? 3 : 2;
        for (int u = 0; u < np; u++)
        {
            int i = u == 0 ? _n0[p] : u == 1 ? _n1[p] : _n2[p];
            double wi = _w[i];
            if (wi <= 0) continue;
            for (int v = 0; v < nq; v++)
            {
                int k = v == 0 ? _n0[q] : v == 1 ? _n1[q] : _n2[q];
                if (k != i) continue;
                int gp = p * 6 + 2 * u, gq = q * 6 + 2 * v;
                sum += wi * (_g[gp] * _g[gq] + _g[gp + 1] * _g[gq + 1]);
            }
        }
        return sum;
    }

    private void Assemble()
    {
        for (int p = 0; p < _count; p++)
        {
            int row = p * (B + 1);
            if (!_active[p])
            {
                _band[row] = 1;
                for (int k = 1; k <= B; k++) _band[row + k] = 0;
                _rhs[p] = 0;
                continue;
            }
            double scale = 1 / (1 + _gamma[p]);
            // Damping term γ·∇C·(x − x_prev).
            double dC = 0;
            int n = _kind[p] == 1 ? 3 : 2;
            for (int u = 0; u < n; u++)
            {
                int i = u == 0 ? _n0[p] : u == 1 ? _n1[p] : _n2[p];
                dC += _g[p * 6 + 2 * u] * (_x[i] - _px[i]) + _g[p * 6 + 2 * u + 1] * (_y[i] - _py[i]);
            }
            _band[row] = Couple(p, p) + _alpha[p] * scale;
            _rhs[p] = (-_c[p] - _alpha[p] * _lambda[p] - _gamma[p] * dC) * scale;
            for (int k = 1; k <= B; k++)
            {
                int q = p - k;
                _band[row + k] = q >= 0 && _active[q] ? Couple(p, q) : 0f;
            }
        }
    }

    /// <summary>In-place banded Cholesky: band becomes L (row p, offset k = L[p, p−k]).</summary>
    private bool Factor()
    {
        for (int j = 0; j < _count; j++)
        {
            int rowJ = j * (B + 1);
            double s = _band[rowJ];
            for (int k = 1; k <= B && j - k >= 0; k++) s -= _band[rowJ + k] * _band[rowJ + k];
            if (s <= 1e-300) s = 1e-300;
            double ljj = Math.Sqrt(s);
            _band[rowJ] = ljj;
            for (int i = j + 1; i <= Math.Min(_count - 1, j + B); i++)
            {
                int rowI = i * (B + 1);
                int off = i - j;
                double v = _band[rowI + off];
                // Subtract Σ_k L[i,k]·L[j,k] for k < j within both bands.
                for (int k = Math.Max(0, i - B); k < j; k++)
                {
                    if (j - k > B) continue;
                    v -= _band[rowI + (i - k)] * _band[rowJ + (j - k)];
                }
                _band[rowI + off] = v / ljj;
            }
        }
        return true;
    }

    private void SolveBand()
    {
        // L·y = rhs
        for (int i = 0; i < _count; i++)
        {
            int row = i * (B + 1);
            double v = _rhs[i];
            for (int k = 1; k <= B && i - k >= 0; k++) v -= _band[row + k] * _tmp[i - k];
            _tmp[i] = v / _band[row];
        }
        // Lᵀ·x = y
        for (int i = _count - 1; i >= 0; i--)
        {
            double v = _tmp[i];
            for (int k = 1; k <= B && i + k < _count; k++) v -= _band[(i + k) * (B + 1) + k] * _dl[i + k];
            _dl[i] = v / _band[i * (B + 1)];
        }
    }

    private void Apply()
    {
        for (int p = 0; p < _count; p++)
        {
            if (!_active[p]) continue;
            double dl = _dl[p];
            _lambda[p] += dl;
            int n = _kind[p] == 1 ? 3 : 2;
            for (int u = 0; u < n; u++)
            {
                int i = u == 0 ? _n0[p] : u == 1 ? _n1[p] : _n2[p];
                double wi = _w[i];
                if (wi <= 0) continue;
                _x[i] += wi * _g[p * 6 + 2 * u] * dl;
                _y[i] += wi * _g[p * 6 + 2 * u + 1] * dl;
            }
        }
    }
}
