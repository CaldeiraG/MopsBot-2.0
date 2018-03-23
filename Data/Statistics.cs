﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Data
{
    /// <summary>
    /// A class that keeps track of how many characters have been recieved each day
    /// </summary>
    public class Statistics
    {
        public Dictionary<string, int> Days = new Dictionary<string, int>();
        public string today;

        /// <summary>
        /// Initialises Statistics, by reading from a text file containing Date and Characters Count and adding them to a List
        /// </summary>
        public Statistics()
        {
            
            StreamReader read = new StreamReader(new FileStream("mopsdata//statistics.txt", FileMode.OpenOrCreate));
            
            string s = "";

            while ((s = read.ReadLine()) != null)
            {
                string[] data = s.Split(':');
                Days.Add(data[0], int.Parse(data[1]));
            }
            
            read.Dispose();
        }

        /// <summary>
        /// Adds the "increase" parameter to todays value
        /// </summary>
        /// <param name="increase">Integer repesenting how many characters have been recieved</param>
        public void AddValue(int increase)
        {
            today = DateTime.Today.ToString("dd.MM.yyyy");

            if (Days.ContainsKey(today))
                Days[today] += increase;

            else Days.Add(today, increase);

            saveData();
        }

        /// <summary>
        /// Writes all Days and values into a text file
        /// </summary>
        private void saveData()
        {
            StreamWriter write = new StreamWriter(new FileStream("mopsdata//statistics.txt", FileMode.Create));
            write.AutoFlush=true;
            foreach(string cur in Days.Keys)
            {
                write.WriteLine($"{cur}:{Days[cur]}");
            }

            write.Dispose();
            
        }

        /// <summary>
        /// Creates an ASCII chart presenting the past "count" Days and their values
        /// </summary>
        /// <param name="count">Integer, representing how many Days should be shown</param>
        /// <returns></returns>
        public string DrawDiagram(int count)
        {
            var tempDays = (from entry in Days orderby DateTime.ParseExact(entry.Key, "dd/MM/yyyy", null) descending select entry).Take(count).ToArray();
            int maximum = (from entry in tempDays orderby entry.Value descending select entry).ToArray()[0].Value;

            string[] lines = new string[count];

            for(int i = 0; i < count; i++)
            {
                lines[i] = $"{tempDays[i].Key}|";
                double relPercent = tempDays[i].Value / ((double)maximum / 10);
                for(int j = 0; j < relPercent; j++)
                {
                    lines[i] += "■";
                }
                lines[i] += $" {tempDays[i].Value}";
            }

            string output = "```coq\n" + string.Join("\n", lines) + "```";

            return output;
        }
    }
}
