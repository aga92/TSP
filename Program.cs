using System;

namespace MyTSP
{
    class Program
    {
        static void Main()
        {
            double length1, length2, lengthTsp;
            var data = new Coordinate[] {
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
            var solver = new Solver(0, data);
            var tour = solver.Solve2Tsp(out length1, out length2, out lengthTsp);
            Console.WriteLine("Cost = {0} + {1} \n Tsp = {2}", length1, length2, lengthTsp);
            Console.WriteLine("Tour:");
            foreach (var i in tour)
            {
                Console.Write(i + " -> ");
            }
            Console.WriteLine();
            Console.ReadKey(true);
        }
    }
}
