using MPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Laboratornaya_4
{
    enum Tags { ArrayLengthTag, ArrayTag, MaxValuetag };

    class Program
    {
        static void Main(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                Thread.Sleep(3000);
                Console.WriteLine(" Hello, World! from rank " + Communicator.world.Rank
                  + " (running on " + MPI.Environment.ProcessorName + ")");

                Intracommunicator comm = Communicator.world;
                int rank = comm.Rank;

                int rowLength = 5;
                if (rank == 0)
                {
                    string line;
                    List<int[]> elements = new List<int[]>();
                    StreamReader file = new StreamReader(@"matrix.txt");
                    bool isFirstLine = true;
                    int length = 0;
                    while ((line = file.ReadLine()) != null)
                    {
                        var numbers = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => Int32.Parse(x)).ToArray();
                        if (isFirstLine)
                        {
                            isFirstLine = !isFirstLine;
                            length = numbers.Length;
                        }
                        else if (numbers.Length != length)
                        {
                            throw new ArgumentException("УААААЙ НЕПРАВИЛЬНО ВВЕЛ ЦИФРЫ!");
                        }
                        elements.Add(numbers);
                    }
                    file.Close();

                    int countWorkingThreads = comm.Size;

                    if (countWorkingThreads <= 1)
                    {
                        System.Environment.Exit(0);
                    }

                    List<int> multiplications = new List<int>();
                    int currentRankToSend = 0;
                    foreach (var matrixLine in elements)
                    {
                        if (++currentRankToSend > countWorkingThreads - 1)
                            currentRankToSend = 1;

                        comm.Send(matrixLine.Count(), currentRankToSend, (int)Tags.ArrayLengthTag);
                        comm.Send(matrixLine, currentRankToSend, (int)Tags.ArrayTag);
                    }

                    currentRankToSend = 0;
                    for(int i = 0; i < elements.Count - 1; i++)
                    {
                        if (++currentRankToSend > countWorkingThreads - 1)
                            currentRankToSend = 1;

                        int value;
                        comm.Receive<int>(currentRankToSend, (int)Tags.MaxValuetag, out value);
                        
                        multiplications.Add(value);
                    }

                    int counter = 0;
                    Console.WriteLine("=========================================");

                    long sum = 0;
                    foreach (int item in multiplications)
                    {
                        sum += item;
                    }
                    Console.Write("The sum: ");
                    Console.Write(sum.ToString());
                }
                else
                {
                    while (true)
                    {
                        int length = 0;
                        comm.Receive<int>(0, (int)Tags.ArrayLengthTag, out length);
                        Console.WriteLine(comm.Rank + " received length " + length);
                        int[] arrayOfHalfNumbers = new int[length];
                        comm.Receive<int>(0, (int)Tags.ArrayTag, ref arrayOfHalfNumbers);

                        int multiplication = 1;
                        for (int i = 0; i < arrayOfHalfNumbers.Length; i++)
                        {
                            multiplication *= arrayOfHalfNumbers[i];
                        }
                        comm.Send(multiplication, 0, (int)Tags.MaxValuetag);
                        Console.WriteLine(comm.Rank + " send its multiplication " + multiplication);
                    }
                }
            }
        }
    }
}
