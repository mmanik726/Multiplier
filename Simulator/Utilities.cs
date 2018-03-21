using CoinbaseExchange.NET.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static Simulator.Simulator;

namespace Simulator
{

    public class CrossData
    {
        public int sl; 
        public DateTime dt { get; set; }
        public decimal CrossingPrice { get; set; }
        public string Action { get; set; }
    }

    public class Utilities
    {
        

        public static async Task<List<CrossData>> Getcrossings(IEnumerable<DataPoint> macdDtPts, DateTime simStartDate, DateTime simEndDate, int smaOfSmaLen = 2, int counter = 0)
        {
            int L_SIGNAL_LEN = smaOfSmaLen;// 2;//5;//10;
            //const int S_SIGNAL_LEN = 5;

            var strt = macdDtPts.First().dt;
            var end = macdDtPts.Last().dt;

            //Console.WriteLine(smaDtPts.Count());
            macdDtPts = macdDtPts.Where(a => a.dt >= simStartDate && a.dt < simEndDate);
            //Console.WriteLine(smaDtPts.Count());

            

            var bigSmaOfMacd = macdDtPts.Select(d => d.diff).ToList().SMA(L_SIGNAL_LEN);
            //var smallSmaOfMacd = smaDtPts.Select(d => d.diff).ToList().SMA(S_SIGNAL_LEN);

            
            var crossList = new List<CrossData>();

            for (int i = 0; i < bigSmaOfMacd.Count(); i++)
            {
                if (i < L_SIGNAL_LEN)
                {
                    continue;
                }

                Vector intersection;

                var p1 = new Vector(i, bigSmaOfMacd.ElementAt(i - 1));
                var p2 = new Vector(i + 1, bigSmaOfMacd.ElementAt(i));

                var q1 = new Vector(i, macdDtPts.ElementAt(i - 1).diff);
                var q2 = new Vector(i + 1, macdDtPts.ElementAt(i).diff);


                if ((LineSegementsIntersect(p1, p2, q1, q2, out intersection)))
                {
                    var a = (p2.Y > q2.Y) ?  "buy" : "sell" ;

                    counter++;
                    crossList.Add(new CrossData
                    {
                        dt = macdDtPts.ElementAt(i).dt,
                        CrossingPrice = macdDtPts.ElementAt(i).ActualPrice,
                        Action = a
                        //sl = counter
                    });
                }



            }


            return crossList;





        }


        public static bool LineSegementsIntersect(Vector p, Vector p2, Vector q, Vector q2,
            out Vector intersection, bool considerCollinearOverlapAsIntersect = false)
        {
            intersection = new Vector();

            var r = p2 - p;
            var s = q2 - q;
            var rxs = r.Cross(s);
            var qpxr = (q - p).Cross(r);

            // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
            if (rxs.IsZero() && qpxr.IsZero())
            {
                // 1. If either  0 <= (q - p) * r <= r * r or 0 <= (p - q) * s <= * s
                // then the two lines are overlapping,
                if (considerCollinearOverlapAsIntersect)
                    if ((0 <= (q - p) * r && (q - p) * r <= r * r) || (0 <= (p - q) * s && (p - q) * s <= s * s))
                        return true;

                // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
                // then the two lines are collinear but disjoint.
                // No need to implement this expression, as it follows from the expression above.
                return false;
            }

            // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
            if (rxs.IsZero() && !qpxr.IsZero())
                return false;

            // t = (q - p) x s / (r x s)
            var t = (q - p).Cross(s) / rxs;

            // u = (q - p) x r / (r x s)

            var u = (q - p).Cross(r) / rxs;

            // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
            // the two line segments meet at the point p + t r = q + u s.
            if (!rxs.IsZero() && (0 <= t && t <= 1) && (0 <= u && u <= 1))
            {
                // We can calculate the intersection point using either t or u.
                intersection = p + t * r;

                // An intersection was found.
                return true;
            }

            // 5. Otherwise, the two line segments are not parallel but do not intersect.
            return false;
        }
    }


    public static class Extensions
    {
        private const double Epsilon = 1e-10;

        public static bool IsZero(this double d)
        {
            return Math.Abs(d) < Epsilon;
        }
    }

    public class Vector
    {
        public double X;
        public double Y;

        // Constructors.
        public Vector(double x, double y) { X = x; Y = y; }
        public Vector() : this(double.NaN, double.NaN) { }

        public static Vector operator -(Vector v, Vector w)
        {
            return new Vector(v.X - w.X, v.Y - w.Y);
        }

        public static Vector operator +(Vector v, Vector w)
        {
            return new Vector(v.X + w.X, v.Y + w.Y);
        }

        public static double operator *(Vector v, Vector w)
        {
            return v.X * w.X + v.Y * w.Y;
        }

        public static Vector operator *(Vector v, double mult)
        {
            return new Vector(v.X * mult, v.Y * mult);
        }

        public static Vector operator *(double mult, Vector v)
        {
            return new Vector(v.X * mult, v.Y * mult);
        }

        public double Cross(Vector v)
        {
            return X * v.Y - Y * v.X;
        }

        public override bool Equals(object obj)
        {
            var v = (Vector)obj;
            return (X - v.X).IsZero() && (Y - v.Y).IsZero();
        }
    }

}
