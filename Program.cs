using System;

namespace MyTSP
{
    class Program
    {
        static void Main(string[] args)
        {
            double length1, length2, lengthTsp;
            var tour = Solver.Solve2Tsp(out length1, out length2, out lengthTsp);
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
