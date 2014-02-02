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
            Arcs = from p1 in Data
                   from p2 in Data
                   select new Arc { City1 = p1.Name, City2 = p2.Name, Distance = p1.Distance(p2) };
        }

        public Coordinate[] Data { get; set; }
        public int StartCity;
        public IEnumerable<Arc> Arcs { get; private set; }
        public List<int> SolveTspLinearProgramming(out double length)
        {
            SolverContext context = SolverContext.GetContext();
            Model model = context.CreateModel();

            // ------------
            // Parameters
            var city = new Set(Domain.IntegerNonnegative, "city");
            var dist = new Parameter(Domain.Real, "dist", city, city);
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
            length = goal.ToDouble();
            IEnumerable<object> tour = from p in assign.GetValues() where (double)p[0] > 0.9 select p[2];
            return tour.Select(Convert.ToInt32).ToList();
        }

        public List<int> SolveTspNn(out double length)
        {
            int key;
            var result = new Dictionary<int,Pair<int, int>>();
            var myArcs = Arcs.ToList();
            Arc shortestArc = SelectShortest(myArcs);
            var world = Data.Select(item => item.Name).ToDictionary(item => item, item => item == shortestArc.City1||item==shortestArc.City2);  //city, is used
            myArcs.Remove(shortestArc);
            result.Add(shortestArc.City1,new Pair<int, int>{After=shortestArc.City2, Before = shortestArc.City2});
            result.Add(shortestArc.City2, new Pair<int, int> { After = shortestArc.City1, Before = shortestArc.City1 });
            int resultCount = 2;
            length = shortestArc.Distance;
            while (myArcs.Any())
            {
                var arcBetweenSets = myArcs.Where(arc => world[arc.City1] ^ world[arc.City2]).ToList();
                shortestArc = SelectShortest(arcBetweenSets);
                int newCity = world[shortestArc.City1] ? shortestArc.City2 : shortestArc.City1;
                //insert to path
                int start = result.Keys.First();
                int before=0, after=0;
                double increase = Double.MaxValue;
                key = start;
                for (int i = 0; i < resultCount; i++)
                {
                    var a = result[key];
                    var cost = CostOfInsertingNn(Distance(key, a.After), Distance(key, newCity),
                                                 Distance(newCity, a.After));
                    if (cost < increase)
                    {
                        increase = cost;
                        before = key;
                        after = a.After;
                    }
                    key = a.After;
                }

                // before - new city - after
                result.Add(newCity, new Pair<int, int> {After = after, Before = before}); //to jest ok, musi sie ustawic w petli
                result[before].After = newCity;
                result[after].Before = newCity;
                
                //insert to world
                world[newCity] = true;
                
                
                //remove arcs
                myArcs.RemoveAll(arc=>arc.City1==newCity ||arc.City2==newCity);
            }
            var solution = new List<int>{StartCity};
            
            key = result[StartCity].After;
            while (key != StartCity)
            {
               solution.Add(key);
               key = result[key].After;
            }

            return solution;
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

        private double Distance(Coordinate from, Coordinate to)
        {
            return from.Distance(to);
        }

        private Coordinate GetCity(int name)
        {
            return Data.FirstOrDefault(item => item.Name == name);
        }

        private double CostOfInsertingNn(double current, double first, double secound)
        {
            return first + secound - current;
        }

        private Arc SelectShortest(IEnumerable<Arc> data)
        {
            var minDistance = Double.MaxValue;
            Arc newArc = null;
            foreach (var arc in data)
            {
                if (arc.Distance < minDistance)
                {
                    minDistance = arc.Distance;
                    newArc = arc;
                }
            }
            return newArc;
        }
    }

    public class Pair<T1, T2>
    {
        public T1 Before;
        public T2 After;
        
    }
}