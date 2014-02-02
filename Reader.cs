using System;

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
                data[i] = new Coordinate(Convert.ToInt32(tab[0]), Convert.ToInt32(tab[1]), Convert.ToInt32(tab[2]));
            }
            return data;
        }
    }
}