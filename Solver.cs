using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SolverFoundation.Services;

namespace MyTSP
{
    public class Solver
    {
        public Solver(int startCity, Coordinate[] data)
        {
            StartCity = startCity;
            Data = data;
        }

        private Coordinate[] Data;
        private int StartCity;

        public IEnumerable<Arc> Arcs { get; private set; }
        public List<int> SolveTspLinearProgramming(out double length)
        {
            SolverContext context = SolverContext.GetContext();
            Model model = context.CreateModel();

            // ------------
            // Parameters
            var city = new Set(Domain.IntegerNonnegative, "city");
            var dist = new Parameter(Domain.Real, "dist", city, city);
            Arcs = from p1 in Data
                   from p2 in Data
                   select new Arc { City1 = p1.Name, City2 = p2.Name, Distance = p1.Distance(p2) };
            dist.SetBinding(Arcs, "Distance", "City1", "City2");
            model.AddParameters(dist);

            // ------------
            // Decisions
            var assign = new Decision(Domain.IntegerRange(0, 1), "assign", city, city);
            var rank = new Decision(Domain.RealNonnegative, "rank", city);
            model.AddDecisions(assign, rank);

            // ------------
            // Goal: minimize the length of the tour.
            Goal goal = model.AddGoal("TourLength", GoalKind.Minimize,
              Model.Sum(Model.ForEach(city, i => Model.ForEachWhere(city, j => dist[i, j] * assign[i, j], j => i != j))));

            // ------------
            // Enter and leave each city only once.
            int n = Data.Length;
            model.AddConstraint("assign_1",
              Model.ForEach(city, i => Model.Sum(Model.ForEachWhere(city, j => assign[i, j], j => i != j)) == 1));
            model.AddConstraint("assign_2",
              Model.ForEach(city, j => Model.Sum(Model.ForEachWhere(city, i => assign[i, j], i => i != j)) == 1));

            // Forbid subtours (Miller, Tucker, Zemlin - 1960...)
            model.AddConstraint("no_subtours",
              Model.ForEach(city,
                i => Model.ForEachWhere(city,
                  j => rank[i] + 1 <= rank[j] + n * (1 - assign[i, j]),
                  j => Model.And(i != j, i >= 1, j >= 1)
                )
              )
            );

            Solution solution = context.Solve();

            // Retrieve solution information.
            //Console.WriteLine("Cost = {0}", goal.ToDouble());
            length = goal.ToDouble();
            //Console.WriteLine("Tour:");
            IEnumerable<object> tour = from p in assign.GetValues() where (double)p[0] > 0.9 select p[2];
            //foreach (var i in tour.ToArray())
            //{
            //    Console.Write(i + " -> ");
            //}
            //Console.WriteLine();
            return tour.Select(Convert.ToInt32).ToList();
        }

        public List<int> Solve2Tsp(out double length1, out double length2, out double lengthTsp)
        {
            var tour = ChangeCityOrder(SolveTspLinearProgramming(out lengthTsp),StartCity).ToList();
            int lastInCycle = tour.Last();
            int a = lastInCycle; //początek odcinka, który sprawdzamy, czy zamiast niego nie wrócić do miasta startowego
            int? minPoint = null;
            length1 = Double.MaxValue;
            length2 = Double.MaxValue;
            double currentMinimum = Double.MaxValue;
            double lengthFromStartToA = 0;
            foreach (int b in tour)
            {
                if (StartCity == b) //pierwsza iteracja omijana
                {
                    a = b;
                    continue;
                }
                double distanceAb = Distance(a, b);
                double distanceFor1 = Distance(a, StartCity);
                double distanceFor2 = Distance(StartCity, b);
                double distanceChange = distanceFor1 + distanceFor2 - distanceAb;
                if (a != StartCity && b != StartCity)
                {
                    
                    var cost = Delta(lengthFromStartToA+distanceFor1, 
                        lengthTsp - lengthFromStartToA - distanceAb + distanceFor2, 
                        distanceChange);
                    if (cost < currentMinimum)
                    {
                        currentMinimum = cost;
                        minPoint = a;
                        length1 = lengthFromStartToA + distanceFor1;
                        length2 = lengthTsp - lengthFromStartToA - distanceAb + distanceFor2;
                    }
                }
                a = b;
                lengthFromStartToA += distanceAb;
            }
            var solution = new List<int>();
            foreach (int k in tour)
            {
                solution.Add(k);
                if (k == minPoint)
                {
                    solution.Add(StartCity);
                }
            }
            return solution;
        }

        private static double Delta(double length1, double length2, double cycleIncrease)  //funkcja oceny rozwiązania - wg. dróg komiwojażerów i powiększenia cyklu
        {
            return Math.Max(length1, length2); 
                //Math.Abs(length1 - length2) + cycleIncrease;
        }

        private static IEnumerable<int> ChangeCityOrder(List<int> tour, int start)
        {
            var firstPart = tour.SkipWhile(item => item != start);
            var lastPart = tour.TakeWhile(item => item != start);
            return firstPart.Concat(lastPart);
        }


        private double Distance(int from, int to)
        {
            return Data.First(item => item.Name == from).Distance(Data.First(item => item.Name == to));
        }
    }
}