// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

// Undefine this if you want the world to save to the HDD. Disabled by default to avoid littering people's machines when they try the demo.
#define DISABLE_HDD_SYS

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

namespace ironVoxel.Asynchronous {
    public class FileRepository {
        private static Queue<QueuedFileChange> changeQueue;
        private static object changeQueueLock;
        private static Thread thread;
        private static bool shutdown;

        #if UNITY_WEBPLAYER || DISABLE_HDD_SYS
        private static FileDatabase[] fileDatabases = {new InMemoryFileDatabase()};
        #else
        private static FileDatabase[] fileDatabases = {new InMemoryFileDatabase(), new PhysicalMediaFileDatabase()};
        #endif
        
        // Accessed only by the thread:
        private static QueuedFileChange currentChange;
        private static object workPulsePadlock;
        
        public static void InitThreadPool()
        {
            workPulsePadlock = new object();
            currentChange = null;
            changeQueueLock = new object();
            changeQueue = new Queue<QueuedFileChange>(100); // Arbitrary starting size - just so long as some capacity is expected
            shutdown = false;
            
            thread = new Thread(new ThreadStart(ThreadWorker));
            thread.Start();
        }
        
        public static void Push(QueuedFileChange change)
        {
            lock (changeQueueLock) {
                bool doPulse = changeQueue.Count == 0;
                changeQueue.Enqueue(change);
                if (doPulse) {
                    lock (workPulsePadlock) {
                        Monitor.Pulse(workPulsePadlock);
                    }
                }
            }
        }

        public static void Shutdown() {
            shutdown = true;
            while (!QueueIsEmpty()) {
                System.Threading.Thread.Sleep(5);
            }
        }

        private static bool QueueIsEmpty()
        {
            bool returnValue = false;
            lock (changeQueueLock) {
                returnValue = changeQueue.Count == 0;
            }
            return returnValue;
        }
        
        private static QueuedFileChange TryToGetNext()
        {
            QueuedFileChange work = null;
            
            lock (changeQueueLock) {

                while (work == null && changeQueue.Count > 0) {
                    work = changeQueue.Dequeue();

                    // If we're shutting down, throw away any queued loading work
                    if (shutdown == true) {
                        if (work.GetType() == typeof(QueuedFileLoad)) {
                            work = null;
                        }
                    }
                }
            }
            
            return work;
        }
        
        // Accessed only by the thread:
        private static void ThreadWorker()
        {
            while (true) {
                if (HasFileInQueue()) {
                    try {
                        currentChange.Apply(fileDatabases);
                    }
                    catch (Exception e) {
                        Debug.LogError("Async file change error: " + e);
                    }
                    ClearCurrent();
                }
                else {
                    QueuedFileChange work = TryToGetNext();
                    if (work != null) {
                        ChangeCurrent(work);
                    }
                    else {
                        lock (workPulsePadlock) {
                            Monitor.Wait(workPulsePadlock);
                        }
                    }
                }
            }
        }
        
        private static bool HasFileInQueue()
        {
            return currentChange != null;
        }
        
        private static void ChangeCurrent(QueuedFileChange work)
        {
            currentChange = work;
        }
        
        private static void ClearCurrent()
        {
            currentChange = null;
        }
    }
}