using System;
using System.Globalization;

namespace MyTSP
{
    public class Reader
    {
        public Coordinate[] ReadConsoleData()
        {
            int size = Convert.ToInt32(Console.ReadLine());
            var data = new Coordinate[size];
            for (int i = 0; i < size; i++)
            {
                var line = Console.ReadLine();
                var tab = line.Split(' ');
                double coord1;
                double coord2;
                try
                {
                    coord1 = Convert.ToDouble(tab[1], CultureInfo.InvariantCulture);
                    coord2 = Convert.ToDouble(tab[2], CultureInfo.InvariantCulture);
                    data[i] = new Coordinate(i/*Convert.ToInt32(tab[0])*/, coord1, coord2);
                }
                catch
                {
                    Console.WriteLine("i: {0}, line: {1}",i, line);
                    break;
                }
            }
            return data;
        }
    }
}