using System;
using System.Configuration;
using GeneticsDataAccess;
using KamGenetics2020.Model;
using KBLib.Classes;
using KBLib.Extensions;
using KBLib.Helpers;

namespace TestConsole
{
    class Program
    {
        const int SimDuration = 200;
        private static Simulator _simulator;
        private static World _world;

        private static GeneticsDbContext _db;

        private static Simulator Simulator
        {
            get
            {
                if (_simulator == null)
                {
                    _simulator = new Simulator(SimDuration);
                    _simulator.OnSimulate += World.SimulateSinglePeriod;
                    _simulator.OnSimulate += Report;
                    _simulator.OnSimulate += PersistWorldToDb;
                    //_simulator.OnSimulate += PropertyGridRefresh;
                    _simulator.OnReset += World.Reset;
                    _simulator.OnSimulationCompleted += SimulationCompleted;
                }
                return _simulator;
            }
        }

        static void Main()
        {
           ConsoleHelper.WhiteBackground();
           ConsoleHelper.Black();
           ConsoleHelper.BeginProgram();
            CreateDb();
            DoInitialPersist();
            _startTime = DateTime.Now;
            Simulator.Start();
        }

        private static World World
        {
            get
            {
                if (_world == null)
                {
                    _world = new World();
                }
                return _world;
            }
        }


        private static void SimulationCompleted()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Simulation completed.");
            Console.WriteLine();
            ConsoleHelper.EndProgram();
        }

        private static DateTime _startTime;

        private static void Report()
        {
            var elapsed = DateTime.Now - _startTime;
            var estimatedMilliseconds = elapsed.TotalMilliseconds / World.TimeIdx * (SimDuration-World.TimeIdx);
            var estimatedFinish = XDateTime.MilliSecParseToSec(estimatedMilliseconds);

            ConsoleHelper.Black();
            Console.Write($"{Simulator.TimeIndex}:");
            ConsoleHelper.DarkRed();
            Console.Write($" Population: {World.Population}".PadRight(20));
            ConsoleHelper.DarkGreen();
            Console.Write($" Resources: {World.ResourceLevel:n0}".PadRight(22));
            ConsoleHelper.Black();
            Console.Write($" Dying: {World.LastDyingCount}".PadRight(16));
            ConsoleHelper.White();
            Console.Write($" Born: {World.LastBabyCount}".PadRight(16));
            ConsoleHelper.Red();
            Console.WriteLine($" ETA: {estimatedFinish}");


            ConsoleHelper.Black();
        }

        private static void DoInitialPersist()
        {
            Console.WriteLine("Initialising the data context ...");

            _db.Worlds.Add(World);
            PersistWorldToDb();
        }

        private static void CreateDb()
        {
            Console.WriteLine("Creating DB ...");
            _db = new GeneticsDbContext(GetSqlConnectionString(), enforceDbRecreation: true);
        }

        public static string GetSqlConnectionString()
        {
            var connectionStringAlias = "KamGeneticsLibSqlAlias";
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringAlias].ConnectionString;
            // Create a date time dependent DB name
            connectionString = connectionString.Replace("KamGeneticsLibDbName", GetDynamicDbName());
            return connectionString;
        }

        private static string GetDynamicDbName()
        {
            return $"KamGenetics{DateTime.Now.FullDateTimeParse()}";
        }

        private static void PersistWorldToDb()
        {
//#if DEBUG
//            Console.WriteLine("Performing periodic persist ...");
//#endif
            if (World.TimeIdx % 100 == 0
                || World.TimeIdx >= Simulator.FinishTimeIndex)
            {
                _db.SaveChanges();
            }
        }

    }
}
