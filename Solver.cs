using System;
using System.Linq;
using Microsoft.SolverFoundation.Services;

namespace MyTSP
{
    public class Solver
    {
        private static Coordinate[] data = new Coordinate[] {
      new Coordinate(0, 16.47, 96.10),
      new Coordinate(1, 16.47, 94.44),
      new Coordinate(2, 20.09, 92.54),
      new Coordinate(3, 22.39, 93.37),
      new Coordinate(4, 25.23, 97.24),
      new Coordinate(5, 22.00, 96.05),
      new Coordinate(6, 20.47, 97.02),
      new Coordinate(7, 17.20, 96.29),
      new Coordinate(8, 16.30, 97.38),
      new Coordinate(9, 14.05, 98.12),
      new Coordinate(10, 16.53, 97.38),
      new Coordinate(11, 21.52, 95.59),
      new Coordinate(12, 19.41, 97.13),
      new Coordinate(13, 20.09, 94.55)
    };
        public static void SolveTspLinearProgramming()
        {
            SolverContext context = SolverContext.GetContext();
            Model model = context.CreateModel();

            // ------------
            // Parameters
            Set city = new Set(Domain.IntegerNonnegative, "city");
            Parameter dist = new Parameter(Domain.Real, "dist", city, city);
            var arcs = from p1 in data
                       from p2 in data
                       select new Arc { City1 = p1.Name, City2 = p2.Name, Distance = p1.Distance(p2) };
            dist.SetBinding(arcs, "Distance", "City1", "City2");
            model.AddParameters(dist);

            // ------------
            // Decisions
            Decision assign = new Decision(Domain.IntegerRange(0, 1), "assign", city, city);
            Decision rank = new Decision(Domain.RealNonnegative, "rank", city);
            model.AddDecisions(assign, rank);

            // ------------
            // Goal: minimize the length of the tour.
            Goal goal = model.AddGoal("TourLength", GoalKind.Minimize,
              Model.Sum(Model.ForEach(city, i => Model.ForEachWhere(city, j => dist[i, j] * assign[i, j], j => i != j))));

            // ------------
            // Enter and leave each city only once.
            int N = data.Length;
            model.AddConstraint("assign_1",
              Model.ForEach(city, i => Model.Sum(Model.ForEachWhere(city, j => assign[i, j], j => i != j)) == 1));
            model.AddConstraint("assign_2",
              Model.ForEach(city, j => Model.Sum(Model.ForEachWhere(city, i => assign[i, j], i => i != j)) == 1));

            // Forbid subtours (Miller, Tucker, Zemlin - 1960...)
            model.AddConstraint("no_subtours",
              Model.ForEach(city,
                i => Model.ForEachWhere(city,
                  j => rank[i] + 1 <= rank[j] + N * (1 - assign[i, j]),
                  j => Model.And(i != j, i >= 1, j >= 1)
                )
              )
            );

            Solution solution = context.Solve();

            // Retrieve solution information.
            Console.WriteLine("Cost = {0}", goal.ToDouble());
            Console.WriteLine("Tour:");
            var tour = from p in assign.GetValues() where (double)p[0] > 0.9 select p[2];
            foreach (var i in tour.ToArray())
            {
                Console.Write(i + " -> ");
            }
            Console.WriteLine();
        }
    }
}