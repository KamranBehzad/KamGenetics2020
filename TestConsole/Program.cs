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
        /// *************************************************************************************************************

        private static bool _dbRun = true; // if this is false then it's a debug run only. no need for db data storage
        const int SimDuration = 300;

        /// *************************************************************************************************************

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
            // Init random seed
            int? randomSeed = 1344; // can be any uint value user wishes + null. If null given then every run will be different.
            RandomHelper.InitSeed(randomSeed);

            // Init console
            ConsoleHelper.BlackBackground();
            ConsoleHelper.Cyan();
            ConsoleHelper.BeginProgram();

            // Init DB
            CreateDb();
            DoInitialPersist();

            // Start simulation
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
            var elapsed = DateTime.Now - _startTime;

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Simulation completed.");
            Console.WriteLine($"Duration: {elapsed.TotalSeconds:N} s");
            Console.WriteLine();
            ConsoleHelper.EndProgram();
        }

        private static DateTime _startTime;

        private static void Report()
        {
            var elapsed = DateTime.Now - _startTime;
            var estimatedMilliseconds = elapsed.TotalMilliseconds / World.TimeIdx * (SimDuration - World.TimeIdx);
            var estimatedFinish = XDateTime.MilliSecParseToSec(estimatedMilliseconds);

            ConsoleHelper.Contrast();
            Console.Write($"{Simulator.TimeIndex}:");
            ConsoleHelper.DarkRed();
            Console.Write($" Population: {World.Population}".PadRight(20));
            ConsoleHelper.DarkGreen();
            Console.Write($" Resources: {World.ResourceLevel:n0}".PadRight(22));
            ConsoleHelper.Contrast();
            Console.Write($" Terminated/Born: {World.LastDyingCount}/{World.LastBabyCount}".PadRight(25));
            ConsoleHelper.Red();
            Console.WriteLine($" ETA: {estimatedFinish} s");

            ConsoleHelper.Contrast();
        }

        private static void DoInitialPersist()
        {
            if (_dbRun)
            {
                Console.WriteLine("Initialising the data context ...");

                _db.Worlds.Add(World);
                PersistWorldToDb();
            }
            else
            {
                Console.WriteLine("Debug Run. No DB init.");
            }

            ConsoleHelper.WriteLine();
        }

        private static void CreateDb()
        {
            if (_dbRun)
            {
                Console.WriteLine("Creating DB ...");
                _db = new GeneticsDbContext(GetSqlConnectionString(), enforceDbRecreation: true);
            }
            else
            {
                Console.WriteLine("Debug Run. No DB creation.");
            }
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

        private static void SaveDbChanges()
        {
            if (_dbRun)
            {
                _db.SaveChanges();
            }
        }

        private static void PersistWorldToDb()
        {
            //#if DEBUG
            //         Console.WriteLine("Performing periodic persist ...");
            //#endif
            var dbUpdateInterval = 100;
            if (World.TimeIdx > 0
                && (World.TimeIdx % dbUpdateInterval == 0
                || World.TimeIdx >= Simulator.FinishTimeIndex))
            {
                SaveDbChanges();
            }
        }

    }
}