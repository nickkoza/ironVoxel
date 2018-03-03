// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using ironVoxel.Service;
using ironVoxel;

namespace ironVoxel.Asynchronous {
    public class CPUMediator {
        public static readonly int LOW_PRIORITY = 0;
        public static readonly int MEDIUM_PRIORITY = 500000;
        public static readonly int HIGH_PRIORITY = 1000000;
        
        public struct QueuedBatch {
            public BatchProcessor.WorkFunction workFunction;
            public object contextObject;
            public int priority;
            public Vector3 position;
            
            public QueuedBatch (BatchProcessor.WorkFunction workFunction, object contextObject, int priority, Vector3 position)
            {
                this.workFunction = workFunction;
                this.contextObject = contextObject;
                this.priority = priority;
                this.position = position;
            }
        }
        
        private int numberOfThreads; // More threads will decrease world generation time, but increase hitching
        private BatchProcessor[] threads;
        private SortedList<int, QueuedBatch> batchList;
        private object batchListLock;
        private int rePrioritizationIndex;
        private int expectedMaxCapacity = 5000; // Make sure to leave plenty of head room - this is expensive to expand later

        public object batchPulsePadlock;
        
        public CPUMediator (int numberOfThreads)
        {
            this.numberOfThreads = numberOfThreads;
            this.rePrioritizationIndex = 0;

            batchPulsePadlock = new object();
            
            batchListLock = new object();
            batchList = new SortedList<int, QueuedBatch>(expectedMaxCapacity);
            
            threads = new BatchProcessor[numberOfThreads];
            for (int i = 0; i < numberOfThreads; i++) {
                threads[i] = new BatchProcessor(this);
            }
        }
        
        public void EnqueueBatchForProcessing(BatchProcessor.WorkFunction workFunction, object contextObject, int priority, Vector3 position)
        {
            lock (batchListLock) {
                int listPriority = getListPriority(priority, position);

                QueuedBatch batch;
                batch.workFunction = workFunction;
                batch.contextObject = contextObject;
                batch.priority = priority;
                batch.position = position;
                // TODO -- Replace this with a custom data structure that doesn't require this sort of hacky-resorting.
                while (batchList.ContainsKey(listPriority)) {
                    listPriority++;
                }
                batchList.Add(listPriority, batch);

                if (batchList.Count <= numberOfThreads) {
                    lock (batchPulsePadlock) {
                        Monitor.Pulse(batchPulsePadlock);
                    }
                }
            }
        }

        public bool CancelProcessingRequest(object contextObject)
        {
            bool foundBatch = false;
            lock (batchListLock) {
                int size = batchList.Count;
                int i = 0;
                while (i < size) {
                    if (batchList.Values[i].contextObject == contextObject) {
                        foundBatch = true;
                        batchList.RemoveAt(i);
                        size--;
                    }
                    else {
                        i++;
                    }
                }
            }
            return foundBatch;
        }

        public void RePrioritizeBatches()
        {
            lock (batchListLock) {
                if (rePrioritizationIndex >= batchList.Count) {
                    rePrioritizationIndex = 0;
                }

                // TODO -- This isn't an efficient way to re-prioritize the work -- fix it
                int startIndex = rePrioritizationIndex;
                while (rePrioritizationIndex < batchList.Count &&
                      rePrioritizationIndex < startIndex + Configuration.PERFORMANCE_MAX_THREAD_JOB_LIST_REPRIORITIZE_BATCH) {
                    QueuedBatch batch = batchList.Values[rePrioritizationIndex];
                    int listPriority = getListPriority(batch.priority, batch.position);
                    while (batchList.ContainsKey(listPriority)) {
                        listPriority++;
                    }

                    batchList.RemoveAt(rePrioritizationIndex);
                    batchList.Add(listPriority, batch);
                    rePrioritizationIndex++;
                }
            }
        }

        public int ThreadQueueSize()
        {
            int size = 0;
            lock (batchListLock) {
                size = batchList.Count;
            }
            return size;
        }
        
        public QueuedBatch TryToGetBatch()
        {
            QueuedBatch batch;
            batch.workFunction = null;
            batch.contextObject = null;
            batch.priority = 0;
            batch.position = Vector3.zero;
            
            lock (batchListLock) {
                int batchListSize = batchList.Count;
                if (batchListSize > 0) {
                    batch = batchList.Values[batchListSize - 1];
                    batchList.RemoveAt(batchListSize - 1);
                }
            }
            
            return batch;
        }

        public void Shutdown()
        {
            for (int i = 0; i < numberOfThreads; i++) {
                threads[i].Shutdown();
            }
        }

        private int getListPriority(int basePriority, Vector3 batchPosition)
        {
            return basePriority + RenderService.PriorityRelativeToCamera(batchPosition);
        }
    }
}