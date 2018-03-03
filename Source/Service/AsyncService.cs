// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;
using System.Diagnostics;

using System.IO;

using ironVoxel.Asynchronous;

namespace ironVoxel.Service {

    /// <summary>
    /// Handles coordinating all asynchronous interactions with the world.
    /// </summary>
    /// <remarks>
    /// Provides access into world timers, threading, and asynchronous file saving and loading, to keep gameplay going 
    /// smoothly while we do work in the background.
    /// </remarks>
    public sealed class AsyncService : ServiceGateway<AsyncServiceImplementation> {


        /// <summary>
        /// Returns access to the CPU mediator, which is used to schedule work on the CPU.
        /// </summary>
        public static CPUMediator GetCPUMediator()
        {
            return Instance().GetCPUMediator();
        }


        /// <summary>
        /// Reprioritize the CPU work that needs to be done, so that the most urgent jobs get processed first.
        /// </summary>
        public static void RePrioritizeCPUMediatorWork()
        {
            Instance().RePrioritizeCPUMediatorWork();
        }


        /// <summary>
        /// Get how much work is queued for the CPU.
        /// </summary>
        public static int ThreadQueueSize()
        {
            return Instance().ThreadQueueSize();
        }


        /// <summary>
        /// Save the world. (hero outfit optional)
        /// </summary>
        public static void Save()
        {
            Instance().Save();
        }


        /// <summary>
        /// Load the world.
        /// </summary>
        public static void Load()
        {
            Instance().Load();
        }


        /// <summary>
        /// Get whether the world is loading currently.
        /// </summary>
        public static bool Loading()
        {
            return Instance().Loading();
        }


        /// <summary>
        /// Start tracking how long we've spent in the current single frame.
        /// </summary>
        public static void StartFrameTimer()
        {
            Instance().StartFrameTimer();
        }


        /// <summary>
        /// Whether we've exceeded the maximum frame time, and need to bail out of our operation to maintain the 
        /// framerate.
        /// </summary>
        public static bool FrameTimeIsExceeded()
        {
            return Instance().FrameTimeIsExceeded();
        }


        /// <summary>
        /// Whether we have not exceeded the maximum frame time, and can continue doing processing while maintaing the 
        /// framerate.
        /// </summary>
        public static bool FrameTimeIsNotExceeded()
        {
            return Instance().FrameTimeIsNotExceeded();
        }


        /// <summary>
        /// Whether we've exceeded a percentage of the frame time, and know we don't have time to enter a costly
        /// processing operation.
        /// </summary>
        public static bool FrameElapsedPercentageIsExceeded(double percentage)
        {
            return Instance().FrameElapsedPercentageIsExceeded(percentage);
        }


        /// <summary>
        /// Whether we have not exceeded a percentage of the frame time, and know we have time to enter a costly
        /// processing operation.
        /// </summary>
        public static bool FrameElapsedPercentageIsNotExceeded(double percentage)
        {
            return Instance().FrameElapsedPercentageIsNotExceeded(percentage);
        }


        /// <summary>
        /// Get how many milliseconds of the frame has elapsed.
        /// </summary>
        public static double FrameTimeElapsed()
        {
            return Instance().FrameTimeElapsed();
        }


        /// <summary>
        /// Get what percentage of the frame (0.0 to 1.0) has elapsed.
        /// </summary>
        public static double FrameElapsedPercentage()
        {
            return Instance().FrameElapsedPercentage();
        }
    }
    
    public sealed class AsyncServiceImplementation : IService {
        
        private CPUMediator cpuMediator;
        private bool loading = false;
        Stopwatch stopwatch;
        
        public AsyncServiceImplementation ()
        {
            FileRepository.InitThreadPool();
            cpuMediator = new CPUMediator(3);
            stopwatch = new Stopwatch();
        }
        
        public CPUMediator GetCPUMediator()
        {
            return cpuMediator;
        }

        public int ThreadQueueSize()
        {
            return cpuMediator.ThreadQueueSize();
        }
        
        public void StartFrameTimer()
        {
            stopwatch.Reset();
            stopwatch.Start();
        }

        public bool FrameElapsedPercentageIsExceeded(double percentage)
        {
            return FrameElapsedPercentage() >= percentage;
        }

        public bool FrameElapsedPercentageIsNotExceeded(double percentage)
        {
            return !FrameElapsedPercentageIsExceeded(percentage);
        }
        
        public bool FrameTimeIsExceeded()
        {
            return FrameTimeElapsed() > MaxFrameTime();
        }
        
        public bool FrameTimeIsNotExceeded()
        {
            return !FrameTimeIsExceeded();
        }

        public double FrameElapsedPercentage()
        {
            return FrameTimeElapsed() / MaxFrameTime();
        }
        
        public double FrameTimeElapsed()
        {
            return stopwatch.ElapsedMilliseconds;
        }
        
        private double MaxFrameTime()
        {
            return 1000.0 / 60.0;
        }

        public void Save()
        {
            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(SaveFileVersion());
                    writer.Write(World.Instance().GetSeed());
                
                    QueuedFileSave.SaveFinishedCallback callback = new QueuedFileSave.SaveFinishedCallback(SaveCallback);
                    QueuedFileChange change = new QueuedFileSave(FileID(), stream, callback, this);
                    FileRepository.Push(change);
                }
            }
        }
        
        public void Load()
        {
            loading = true;
            QueuedFileLoad.LoadFinishedCallback callback = new QueuedFileLoad.LoadFinishedCallback(LoadCallback);
            QueuedFileChange change = new QueuedFileLoad(FileID(), callback, this);
            FileRepository.Push(change);
        }
        
        public bool Loading()
        {
            return loading;
        }

        public void RePrioritizeCPUMediatorWork()
        {
            cpuMediator.RePrioritizeBatches();
        }
        
        private void SaveCallback(object chunkInstance)
        {
        }
        
        private void LoadCallback(string filePath, MemoryStream stream, object context)
        {
            if (stream == null) {
                Save();
                loading = false;
                return;
            }
            
            using (BinaryReader reader = new BinaryReader(stream)) {
                stream.Seek(0, SeekOrigin.Begin);
                uint version = reader.ReadUInt32();
                if (version != 0) {
                    UnityEngine.Debug.LogError("Unexpected version found in world file: " + version + ".");
                }
                World.Instance().SetSeed(reader.ReadInt32());
            }
            
            loading = false;
        }
        
        private uint SaveFileVersion()
        {
            return 0;
        }
        
        private string FileID()
        {
            return "world";
        }
    }
}
