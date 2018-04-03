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
        public double cossDiff { get; set; }
        public double smaValue { get; set; }
        public string comment { get; set; }
        public double CalculatedBalance { get; set; }
        public double CalculatedNetPL { get; set; }
    }

    public class Utilities
    {
        static object cwWriteLock = new object();

        static string lastAction = ""; 

        public List<CrossData> Getcrossings(IEnumerable<SmaData> macdDtPts, IEnumerable<SmaData> signalDtPts, DateTime simStartDate, DateTime simEndDate, int smaOfSmaLen = 2, int counter = 0)
        {

            int L_SIGNAL_LEN = smaOfSmaLen;// 2;//5;//10;
            //const int S_SIGNAL_LEN = 5;

            var strt = macdDtPts.First().Time;
            var end = macdDtPts.Last().Time;

            var sigstrt = signalDtPts.First().Time;
            var sigend = signalDtPts.Last().Time;

            var macdptsCount = macdDtPts.Count();
            var signalPtCounts = signalDtPts.Count();

            //Console.WriteLine("\t\t" + strt.ToString() + "\t" + end.ToString() + "\n");

            //Console.WriteLine(smaDtPts.Count());
            ///macdDtPts = macdDtPts.Where(a => a.Time >= simStartDate && a.Time < simEndDate);
            //Console.WriteLine(smaDtPts.Count());



            var bigSmaOfMacd = signalDtPts;//macdDtPts.Select(d => d.SmaValue).ToList().SMA(L_SIGNAL_LEN);
            //var smallSmaOfMacd = smaDtPts.Select(d => d.diff).ToList().SMA(S_SIGNAL_LEN);


            var crossList = new List<CrossData>();



            //////var buySells = macdDtPts.Zip(signalDtPts, (macd, signal) => (Math.Abs(macd.SmaValue) - Math.Abs(signal.SmaValue)) ).Select((bs,i) => new SmaData
            //////{
            //////    SmaValue = bs,
            //////    ActualPrice = macdDtPts.ElementAt(i).ActualPrice,
            //////    Time = macdDtPts.ElementAt(i).Time
            //////});


            //////var sells = buySells.Where((bs) => bs.SmaValue > 0).ToList();
            //////var sellWithData = sells.Select((s, i) => new CrossData
            //////{
            //////    dt = buySells.ElementAt(i).Time,
            //////    CrossingPrice = buySells.ElementAt(i).ActualPrice,
            //////    Action = "sell",
            //////    sl = i
            //////}).ToList() ;



            //////var buys = buySells.Where((bs) => bs.SmaValue < 0).Select((b, i) => new CrossData
            //////{
            //////    dt = buySells.ElementAt(i).Time,
            //////    CrossingPrice = buySells.ElementAt(i).ActualPrice,
            //////    Action = "buy",
            //////    sl = i
            //////}).ToList();


            //////var unknowns = buySells.Where((bs) => bs.SmaValue == 0).Select((u, i) => new CrossData
            //////{
            //////    dt = buySells.ElementAt(i).Time,
            //////    CrossingPrice = buySells.ElementAt(i).ActualPrice,
            //////    Action = "unknown",
            //////    sl = i
            //////}).ToList();


            for (int i = 1; i < bigSmaOfMacd.Count(); i++)
            {
                //if (i < L_SIGNAL_LEN)
                //{
                //    continue;
                //}

                Vector intersection;

                var signal_p1 = new Vector(i, Math.Round(bigSmaOfMacd.ElementAt(i - 1).SmaValue, 4));
                var signal_p2 = new Vector(i + 1, Math.Round(bigSmaOfMacd.ElementAt(i).SmaValue, 4));

                var macd_q1 = new Vector(i, Math.Round(macdDtPts.ElementAt(i - 1).SmaValue, 4));
                var macd_q2 = new Vector(i + 1, Math.Round(macdDtPts.ElementAt(i).SmaValue, 4));


                if ((LineSegementsIntersect(signal_p1, signal_p2, macd_q1, macd_q2, out intersection)))
                {



                    var currentAction = (macd_q2.Y > signal_p2.Y) ?  "buy" : "sell" ;

                    var yDiff = Math.Abs(macd_q2.Y) - Math.Abs(signal_p2.Y);

                    //var thisPoint = string.Format("{0} \tsignal_p1({1}) -> signla_p2({2}), macd_q1({3}) -> macd_q2({4} ({5} by {6}))",
                    //    macdDtPts.ElementAt(i).Time,
                    //    Math.Round(bigSmaOfMacd.ElementAt(i - 1).SmaValue, 4),
                    //    Math.Round(bigSmaOfMacd.ElementAt(i).SmaValue, 4),
                    //    Math.Round(macdDtPts.ElementAt(i - 1).SmaValue, 4),
                    //    Math.Round(macdDtPts.ElementAt(i).SmaValue, 4),
                    //    a,
                    //    Math.Round(yDiff, 6));
                    //Console.WriteLine(thisPoint);

                    if (lastAction == currentAction)
                    {
                        var manualEntry = (currentAction == "sell") ? "buy" : "sell";

                        //Console.WriteLine("same last action");
                        counter++;
                        crossList.Add(new CrossData
                        {
                            dt = macdDtPts.ElementAt(i).Time.AddMinutes(-1),
                            CrossingPrice = macdDtPts.ElementAt(i).ActualPrice,
                            Action = manualEntry,
                            sl = counter,
                            cossDiff = Math.Abs(macd_q2.Y) - Math.Abs(signal_p2.Y),
                            comment = "MANUAL_ENTRY",
                            smaValue = intersection.Y

                        });

                        
                    }

                    counter++;
                    crossList.Add(new CrossData
                    {
                        dt = macdDtPts.ElementAt(i).Time,
                        CrossingPrice = macdDtPts.ElementAt(i).ActualPrice,
                        Action = currentAction,
                        sl = counter,
                        cossDiff = Math.Abs(macd_q2.Y) - Math.Abs(signal_p2.Y),
                        smaValue = intersection.Y

                    });

                    lastAction = currentAction;

                }



            }


            lock (cwWriteLock)
            {
                Console.WriteLine("\t\t" + simStartDate.ToString() + "\t" + simEndDate.ToString() + "(" + crossList.Count() +  ")\n");
            }


            return crossList;





        }


        public static void EnterMissingActions(ref List<CrossData> allCrossings)
        {
            allCrossings = allCrossings.OrderBy(s => s.dt).ToList();

            var lastAction = "";
            List<CrossData> ManualEntries = new List<CrossData>();
            for (int i = 0; i < allCrossings.Count(); i++)
            {

                //if last action = current action 
                if (lastAction == allCrossings[i].Action)
                {
                    var manualEntry = (lastAction == "sell") ? "buy" : "sell";
                    //add to list 
                    ManualEntries.Add(new CrossData
                    {
                        dt = allCrossings[i - 1].dt.AddMinutes(1),
                        CrossingPrice = allCrossings[i - 1].CrossingPrice,
                        Action = manualEntry,
                        sl = allCrossings[i - 1].sl + 1,
                        cossDiff = 0,
                        comment = "MANUAL_ENTRY"
                    });

                    //lastAction = manualEntry;
                    //continue;
                }

                lastAction = allCrossings[i].Action;
            }

            allCrossings.AddRange(ManualEntries);

            allCrossings = allCrossings.OrderBy(s => s.dt).ToList();

        }


        public List<CrossData> Getcrossings_Linq(IEnumerable<SmaData> macdDtPts, IEnumerable<SmaData> signalDtPts, DateTime simStartDate, DateTime simEndDate)
        {

            //int L_SIGNAL_LEN = smaOfSmaLen;// 2;//5;//10;
            //const int S_SIGNAL_LEN = 5;

            var strt = macdDtPts.First().Time;
            var end = macdDtPts.Last().Time;

            var sigstrt = signalDtPts.First().Time;
            var sigend = signalDtPts.Last().Time;

            var macdptsCount = macdDtPts.Count();
            var signalPtCounts = signalDtPts.Count();

            //Console.WriteLine("\t\t" + strt.ToString() + "\t" + end.ToString() + "\n");

            //Console.WriteLine(smaDtPts.Count());
            ///macdDtPts = macdDtPts.Where(a => a.Time >= simStartDate && a.Time < simEndDate);
            //Console.WriteLine(smaDtPts.Count());



            //var bigSmaOfMacd = signalDtPts;//macdDtPts.Select(d => d.SmaValue).ToList().SMA(L_SIGNAL_LEN);
            //var smallSmaOfMacd = smaDtPts.Select(d => d.diff).ToList().SMA(S_SIGNAL_LEN);


            var crossList = new List<CrossData>();



            var allBuys = macdDtPts.Where((d, i) => d.SmaValue > signalDtPts.ElementAt(i).SmaValue).ToList();

            var allSells = macdDtPts.Where((d, i) => d.SmaValue < signalDtPts.ElementAt(i).SmaValue).ToList();


            while (allBuys.Count() > 0 || allSells.Count() > 0)
            {

                if (allBuys.Count() == 0)
                {
                    var tempSingleSell = allSells.First();
                    var tempSingleSellData = new CrossData
                    {
                        Action = "sell",
                        dt = tempSingleSell.Time,
                        CrossingPrice = tempSingleSell.ActualPrice,
                        smaValue = tempSingleSell.SmaValue
                    };
                    crossList.Add(tempSingleSellData);

                    break;
                }

                if (allSells.Count() == 0)
                {
                    var tempSingleBuy = allBuys.First();
                    var tempSingleBuyData = new CrossData
                    {
                        Action = "buy",
                        dt = tempSingleBuy.Time,
                        CrossingPrice = tempSingleBuy.ActualPrice,
                        smaValue = tempSingleBuy.SmaValue
                    };
                    crossList.Add(tempSingleBuyData);

                    break;
                }



                var curBuy = allBuys.First();
                var tempBuyData = new CrossData
                {
                    Action = "buy",
                    dt = curBuy.Time,
                    CrossingPrice = curBuy.ActualPrice,
                    smaValue = curBuy.SmaValue
                };
                crossList.Add(tempBuyData);

                
                var curSell = allSells.First();
                var tempSellData = new CrossData
                {
                    Action = "sell",
                    dt = curSell.Time,
                    CrossingPrice = curSell.ActualPrice,
                    smaValue = curSell.SmaValue
                };
                crossList.Add(tempSellData);



                allBuys = allBuys.Where((d) => d.Time > curSell.Time).ToList();
                if (allBuys.Count() > 0)
                {
                    allSells = allSells.Where((d) => d.Time > allBuys.First().Time).ToList();
                }
                else
                {
                    break;
                }
                
            }




            

            lock (cwWriteLock)
            {
                Console.WriteLine("\t\t" + simStartDate.ToString() + "\t" + simEndDate.ToString() + "(" + crossList.Count() + ")\n");
            }

            if (crossList.First().Action == "buy")
            {
                crossList.RemoveAt(0);
            }

            return crossList;

        }



        public List<CrossData> Getcrossings_Parallel(IEnumerable<SmaData> macdDtPts, IEnumerable<SmaData> signalDtPts, DateTime simStartDate, DateTime simEndDate, int smaOfSmaLen = 2, int counter = 0)
        {

            int L_SIGNAL_LEN = smaOfSmaLen;// 2;//5;//10;
            //const int S_SIGNAL_LEN = 5;

            var strt = macdDtPts.First().Time;
            var end = macdDtPts.Last().Time;

            var sigstrt = signalDtPts.First().Time;
            var sigend = signalDtPts.Last().Time;

            var macdptsCount = macdDtPts.Count();
            var signalPtCounts = signalDtPts.Count();

            //Console.WriteLine("\t\t" + strt.ToString() + "\t" + end.ToString() + "\n");

            //Console.WriteLine(smaDtPts.Count());
            ///macdDtPts = macdDtPts.Where(a => a.Time >= simStartDate && a.Time < simEndDate);
            //Console.WriteLine(smaDtPts.Count());



            var bigSmaOfMacd = signalDtPts;//macdDtPts.Select(d => d.SmaValue).ToList().SMA(L_SIGNAL_LEN);
            //var smallSmaOfMacd = smaDtPts.Select(d => d.diff).ToList().SMA(S_SIGNAL_LEN);


            var crossList = new List<CrossData>();




            for (int i = 1; i < bigSmaOfMacd.Count(); i++)
            {
                //if (i < L_SIGNAL_LEN)
                //{
                //    continue;
                //}

                Vector intersection;

                var signal_p1 = new Vector(i, Math.Round(bigSmaOfMacd.ElementAt(i - 1).SmaValue, 4));
                var signal_p2 = new Vector(i + 1, Math.Round(bigSmaOfMacd.ElementAt(i).SmaValue, 4));

                var macd_q1 = new Vector(i, Math.Round(macdDtPts.ElementAt(i - 1).SmaValue, 4));
                var macd_q2 = new Vector(i + 1, Math.Round(macdDtPts.ElementAt(i).SmaValue, 4));


                if ((LineSegementsIntersect(signal_p1, signal_p2, macd_q1, macd_q2, out intersection)))
                {



                    var currentAction = (macd_q2.Y > signal_p2.Y) ? "buy" : "sell";

                    var yDiff = Math.Abs(macd_q2.Y) - Math.Abs(signal_p2.Y);


                    counter++;
                    crossList.Add(new CrossData
                    {
                        dt = macdDtPts.ElementAt(i).Time,
                        CrossingPrice = macdDtPts.ElementAt(i).ActualPrice,
                        Action = currentAction,
                        sl = counter,
                        cossDiff = Math.Abs(macd_q2.Y) - Math.Abs(signal_p2.Y),
                        smaValue = intersection.Y
                        

                    });

                    lastAction = currentAction;

                }



            }


            lock (cwWriteLock)
            {
                Console.WriteLine("\t\t" + simStartDate.ToString() + "\t" + simEndDate.ToString() + "(" + crossList.Count() + ")\n");
            }


            return crossList;





        }


        public bool LineSegementsIntersect(Vector p, Vector p2, Vector q, Vector q2,
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
