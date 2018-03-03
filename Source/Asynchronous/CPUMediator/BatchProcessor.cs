// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System;
using System.Threading;

namespace ironVoxel.Asynchronous {
    public class BatchProcessor {
        public delegate void WorkFunction(object contextObject);
        
        private object padlock;
        private WorkFunction currentWorkFunction;
        private object currentContext;
        private Thread thread;
        private CPUMediator cpuMediator;
        private bool shutdown;
        
        public BatchProcessor (CPUMediator cpuMediator)
        {
            padlock = new object();
            shutdown = false;
            this.cpuMediator = cpuMediator;
            currentWorkFunction = null;
            currentContext = null;
            thread = new Thread(new ThreadStart(DoProcessing));
            thread.Start();
        }
        
        public void DoProcessing()
        {
            while (shutdown == false) {
                if (HasWork()) {
                    try {
                        currentWorkFunction(currentContext);
                    }
                    catch (Exception e) {
                        Debug.LogError("Thread error: " + e);
                    }
                    ClearWork();
                }
                else {
                    lock (cpuMediator.batchPulsePadlock) {
                        Monitor.Wait(cpuMediator.batchPulsePadlock);
                    }
                }
                
                CheckForMoreWork();
            }
        }
        
        public bool TryToStartWork(WorkFunction workFunction, object contextObject)
        {
            bool returnValue;
            lock (padlock) {
                if (currentWorkFunction == null) {
                    currentWorkFunction = workFunction;
                    currentContext = contextObject;
                    returnValue = true;
                }
                else {
                    returnValue = false;
                }
            }
            return returnValue;
        }
        
        public bool HasWork()
        {
            bool hasWork;
            lock (padlock) {
                hasWork = currentWorkFunction != null;
            }
            return hasWork;
        }

        public void Shutdown()
        {
            shutdown = true;
        }
        
        private void SetWork(WorkFunction workFunction, object contextObject)
        {
            lock (padlock) {
                currentWorkFunction = workFunction;
                currentContext = contextObject;
            }
        }
        
        private void ClearWork()
        {
            lock (padlock) {
                currentWorkFunction = null;
                currentContext = null;
            }
        }
        
        private void CheckForMoreWork()
        {
            CPUMediator.QueuedBatch batch;
            lock (padlock) {
                batch = cpuMediator.TryToGetBatch();
            }
            
            if (batch.workFunction != null) {
                currentWorkFunction = batch.workFunction;
                currentContext = batch.contextObject;
            }
        }
    }
}