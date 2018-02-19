using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MopsBot.Data.Session
{
    public class Crosswords
    {
        public string[] words;
        public List<Word> guessWords;
        public Tuple<direction, char>[,] mapset;
        private Random decider;


        public Crosswords(string[] pWords)
        {
            decider = new Random();

            words = pWords;

            int fieldSize = 0;

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].ToLower().Equals("random")) words[i] = MopsBot.Module.Information.getRandomWord();
                if (words[i].Length + 3 > fieldSize) fieldSize = words[i].Length + 3;
            }

            mapset = new Tuple<direction, char>[fieldSize, fieldSize];
            fillField();

            guessWords = new List<Word>();

            allocate();
        }

        private void fillField()
        {
            for (int i = 0; i < mapset.GetLength(0); i++)
            {
                for (int j = 0; j < mapset.GetLength(1); j++)
                    mapset[i, j] = new Tuple<direction, char>(direction.UnAllocated, (char)decider.Next(65, 91));
            }
        }

        private void allocate()
        {
            foreach (string word in words)
            {
                int x = 0;
                int y = 0;

                direction wordDirection = direction.UnAllocated;

                switch (decider.Next(0, 2))
                {
                    case 0:
                        wordDirection = direction.Right;
                        x = decider.Next(mapset.GetLength(0) - word.Length);
                        y = decider.Next(mapset.GetLength(0));
                        break;
                    case 1:
                        wordDirection = direction.Down;
                        x = decider.Next(mapset.GetLength(0));
                        y = decider.Next(mapset.GetLength(0) - word.Length);
                        break;
                }

                int errorCount = 0;
                int delayCount = 0;
                bool shift = false;
                while (!isPlacable(word, wordDirection, x, y))
                {
                    errorCount++;
                    delayCount++;
                    if(errorCount > mapset.GetLength(0) + words.Length * 4){
                        mapset = new Tuple<direction, char>[mapset.GetLength(0) + 4, mapset.GetLength(0) + 4];
                        fillField();
                        allocate();
                    }

                    switch (wordDirection)
                    {
                        case direction.Right:
                            if(!shift)
                                if(y < mapset.GetLength(0) - 1)
                                    y++;
                                else{
                                    shift = true;
                                    y -= delayCount -1;
                                }

                            else{
                                if(x > 0)
                                    y--;
                                else{
                                    shift = false;
                                    wordDirection = direction.Down;
                                    y = decider.Next(mapset.GetLength(0) - word.Length);
                                    delayCount = 0;
                                }
                            }
                            break;

                        case direction.Down:
                            if(!shift)
                                if(x < mapset.GetLength(0) - 1)
                                    x++;
                                else{
                                    shift = true;
                                    x -= delayCount -1;
                                }

                            else{
                                if(x > 0)
                                    x--;
                                else{
                                    shift = false;
                                    wordDirection = direction.Right;
                                    x = decider.Next(mapset.GetLength(0) - word.Length);
                                    delayCount = 0;
                                }
                            }

                            break;
                    }
                }

                placeWord(word, wordDirection, x, y);
            

                Console.WriteLine(word + " going " + wordDirection.ToString());
            }
        }

        private void placeWord(string word, direction dir, int x, int y)
        {
            if(dir.Equals(direction.Right))
                guessWords.Add(new Word(x, y, x + word.Length - 1, y, word));
            else 
                guessWords.Add(new Word(x, y, x, y + word.Length - 1, word));

            for (int i = 0; i < word.Length; i++)
            {
                switch (dir)
                {
                    case direction.Right:
                        mapset[x + i, y] = new Tuple<direction, char>(dir, char.ToUpper(word[i]));
                        break;
                    case direction.Down:
                        mapset[x, y + i] = new Tuple<direction, char>(dir, char.ToUpper(word[i]));
                        break;
                }
            }
        }

        private bool isPlacable(string word, direction dir, int x, int y)
        {
            for (int i = 0; i < word.Length; i++)
            {
                switch (dir)
                {
                    case direction.Right:
                        if (!mapset[x + i, y].Item1.Equals(direction.UnAllocated)
                            && mapset[x + i, y].Item2 != (word[i]))
                            return false;
                        break;
                    case direction.Down:
                        if (!mapset[x, y+i].Item1.Equals(direction.UnAllocated)
                            && mapset[x, y+i].Item2 != (word[i]))
                            return false;
                        break;
                }
            }
            return true;
        }

        public string drawMap()
        {
            string[] lines = new string[mapset.GetLength(1) + 1];

            lines[0] = "    ";

            for (int i = 0; i < mapset.GetLength(0); i++)
            {
                lines[0] += i.ToString().Length < 2 ? $"{i}  " : $"{i} ";
                lines[i + 1] += i.ToString().Length < 2 ? $"{i}  " : $"{i} ";
                for (int j = 0; j < mapset.GetLength(1); j++)
                {
                    lines[i + 1] += $"[{mapset[j, i].Item2}]";
                }
            }

            return "```\n" +
                    $"{String.Join("\n", lines)}" +
                    $"\n```\nA total of {guessWords.Count} words are being searched. Good luck :d";
        }

        public string guessWord(ulong pUser, string guess)
        {
            var points = guess.Split(' ', ':');

            Word tempWord = guessWords.Where(x => x.Xstart == int.Parse(points[0]) && x.Ystart == int.Parse(points[1]) && x.Xend == int.Parse(points[2]) && x.Yend == int.Parse(points[3])).ElementAt(0);

            if (!string.IsNullOrEmpty(tempWord.word))
            {
                guessWords.Remove(tempWord);
                StaticBase.people.addStat(pUser, (mapset.GetLength(0) - (tempWord.word.Length - 1)) * tempWord.word.Length, "Score");
                return $"Yes! You found {tempWord.word} ({guessWords.Count} words remaining)\n+**[$ {(mapset.GetLength(0) - (tempWord.word.Length - 1)) * tempWord.word.Length} $]**";
            }

            else return "Nope";
        }

        public enum direction { Right, Down, DownRight, UpRight, UnAllocated };
    }

    public class Word
    {
        public string word;
        public int Xstart, Ystart, Xend, Yend;

        public Word(int pStartx, int pStarty, int pEndx, int pEndy, string pWord)
        {
            Xstart = pStartx;
            Ystart = pStarty;
            Xend = pEndx;
            Yend = pEndy;
            word = pWord;
        }
    }
}