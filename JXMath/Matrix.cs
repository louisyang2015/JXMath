using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXMath
{
    public class Matrix
    {
        double[,] _m;

        public int Rows
        {
            get { return _m.GetLength(0); }
        }

        public int Cols
        {
            get { return _m.GetLength(1); }
        }

        public double this[int r, int c]
        {
            get { return _m[r, c]; }
            set { _m[r, c] = value; }
        }


        #region Constructors

        public Matrix(int rows, int cols)
        {
            _m = new double[rows, cols];
        }

        public Matrix(int rows, int cols, double value)
        {
            _m = new double[rows, cols];

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    _m[r, c] = value;
        }

        public Matrix(byte[,] bytes)
        {
            _m = new double[bytes.GetLength(0), bytes.GetLength(1)];

            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    _m[r, c] = bytes[r, c];
        }

        public Matrix(double[,] m)
        {
            _m = m;
        }

        /// <summary>
        /// Fill matrix with column vectors.
        /// </summary>
        public Matrix(params Vector[] vectors)
        {
            _m = new double[vectors[0].Length, vectors.Length];

            for (int c = 0; c < vectors.Length; c++)
                for (int r = 0; r < vectors[0].Length; r++)
                    _m[r, c] = vectors[c][r];
        }

        public Matrix Clone()
        {
            var result = new Matrix(Rows, Cols);

            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    result[r, c] = _m[r, c];

            return result;
        }

        #endregion


        #region Methods

        public void ApplyFunction(Func<double, double> func)
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    _m[r, c] = func(_m[r, c]);
        }

        public Matrix Convolve(Matrix mask)
        {
            if (mask.Rows % 2 == 0 || mask.Cols % 2 == 0)
                throw new ArgumentException("Mask dimensions must be odd.");

            int half_height = mask.Rows / 2;
            int half_width = mask.Cols / 2;

            Matrix result = new Matrix(Rows, Cols);

            Parallel.For(0, Rows, r =>
            {
                if (r < half_height || r >= Rows - half_height)
                {
                    // No convolve for the whole row
                    for (int c = 0; c < Cols; c++)
                        result[r, c] = _m[r, c];

                    return;
                }

                for (int c = 0; c < Cols; c++)
                {
                    if (c < half_width || c >= Cols - half_width)
                    {
                        // No convolve
                        result[r, c] = _m[r, c];
                    }
                    else
                    {
                        double sum = 0;

                        for (int i = -half_height; i <= half_height; i++)
                            for (int j = -half_width; j <= half_width; j++)
                            {
                                int r2 = r + i;
                                int c2 = c + j;

                                sum += _m[r2, c2] * mask[i + half_height, j + half_width];
                            }

                        result[r, c] = sum;
                    }
                }
            });

            return result;
        }

        /// <summary>
        /// Limit the values of the matrix to between low and high.
        /// </summary>
        public void Limit(double low, double high)
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    if (_m[r, c] < low)
                        _m[r, c] = low;
                    else if (_m[r, c] > high)
                        _m[r, c] = high;
                }
        }

        public double Sum()
        {
            double sum = 0;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    sum += _m[r, c];

            return sum;
        }

        public Matrix Transpose()
        {
            var result = new Matrix(Cols, Rows);

            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    result[c, r] = _m[r, c];

            return result;
        }

        #endregion


        #region Operators

        public static Matrix operator +(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Cols != b.Cols)
                throw new ArgumentException("Matrices must have the same dimensions.");

            var result = new Matrix(a.Rows, a.Cols);

            for (int r = 0; r < a.Rows; r++)
                for (int c = 0; c < a.Cols; c++)
                    result[r, c] = a[r, c] + b[r, c];

            return result;
        }

        /*
        public static Matrix operator +(Matrix a, RandomVariable rv)
        {
            var result = new Matrix(a.Rows, a.Cols);
            var values = rv.Sample(a.Rows * a.Cols);

            for (int r = 0; r < a.Rows; r++)
                for (int c = 0; c < a.Cols; c++)
                {
                    result[r, c] = a[r, c] + values[r * a.Cols + c];
                }

            return result;
        }
        */

        public static Matrix operator *(double b, Matrix a)
        {
            var result = new Matrix(a.Rows, a.Cols);

            for (int r = 0; r < a.Rows; r++)
                for (int c = 0; c < a.Cols; c++)
                    result[r, c] = b * a[r, c];

            return result;
        }

        public static Matrix operator /(Matrix a, double b)
        {
            double inv_b = 1.0 / b;
            return inv_b * a;
        }

        #endregion


        #region Output

        public byte[,] ToBytes()
        {
            var result = new byte[Rows, Cols];

            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    if (_m[r, c] < 0)
                        result[r, c] = 0;
                    else if (_m[r, c] > 255)
                        result[r, c] = 255;
                    else
                        result[r, c] = (byte)Math.Round(_m[r, c]);
                }

            return result;
        }

        public string ToTsv()
        {
            StringBuilder sb = new StringBuilder();

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    sb.Append(_m[r, c]);
                    if (c < Cols - 1)
                        sb.Append('\t');
                }

                if (r < Rows - 1)
                    sb.Append('\n');
            }

            return sb.ToString();
        }

        #endregion
    }



    public class Vector
    {
        double[] _v;
                
        public double this[int i]
        {
            get { return _v[i]; }
            set { _v[i] = value; }
        }

        public int Length
        {
            get { return _v.Length; }
        }

        public double[] Array
        {
            get { return _v; }
        }


        #region Constructors

        public Vector(params double[] v)
        {
            _v = v;
        }

        public static Vector MakeSequence(double start, double stop, double step = 1)
        {
            int n = (int)((stop - start) / step) + 1;
            var v = new double[n];

            for (int i = 0; i < n; i++)
                v[i] = start + i * step;

            return new Vector(v);
        }

        #endregion


        #region Methods

        public double Sum()
        {
            double sum = 0;
            for (int i = 0; i < Length; i++)
                sum += _v[i];

            return sum;
        }

        #endregion


        #region Math Operators

        public static Vector operator +(Vector a, Vector b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same length.");

            double[] v = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
                v[i] = a[i] + b[i];

            return new Vector(v);
        }

        public static Vector operator -(Vector a, Vector b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have the same length.");

            double[] v = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
                v[i] = a[i] - b[i];

            return new Vector(v);
        }

        public static Vector operator *(double b, Vector a)
        {
            double[] v = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
                v[i] = b * a[i];

            return new Vector(v);
        }

        public static Vector operator /(Vector a, double b)
        {
            double inv_b = 1.0 / b;
            return inv_b * a;
        }

        #endregion


        #region Conversion Operators

        public static implicit operator Vector(double[] v)
        {
            return new Vector(v);
        }

        public static implicit operator double[](Vector v)
        {
            return v.Array;
        }

        public static implicit operator Vector((double, double) t)
        {
            return new Vector(t.Item1, t.Item2);
        }

        public static implicit operator Vector((double, double, double) t)
        {
            return new Vector(t.Item1, t.Item2, t.Item3);
        }

        public static implicit operator (double, double)(Vector v)
        {
            if (v.Length != 2)
                throw new ArgumentException("Vector must have length 2.");

            return (v[0], v[1]);
        }

        public static implicit operator (double, double, double)(Vector v)
        {
            if (v.Length != 3)
                throw new ArgumentException("Vector must have length 3.");

            return (v[0], v[1], v[2]);
        }

        #endregion


        #region Conversion Functions

        public override string ToString()
        {
            return "<" + string.Join(", ", _v) + ">";
        }

        public string ToTsv()
        {
            // Column vector format
            return string.Join("\n", _v);
        }

        #endregion
    }


    public static class VectorExtensions
    {
        public static Vector Eval(this Func<double, double> func, Vector v)
        {
            var result = new double[v.Length];
            Parallel.For(0, v.Length, i =>
            {
                result[i] = func(v[i]);
            });

            return new Vector(result);
        }

        public static Vector Eval(this Vector v, Func<double, double> func)
        {
            return func.Eval(v);
        }
    }

}
