// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel.Asynchronous {
    public class InMemoryFileDatabase : FileDatabase {
        object padlock;
        protected SortedDictionary<string, MemoryStream> streamContainer;

        public InMemoryFileDatabase ()
        {
            padlock = new object();
            streamContainer = new SortedDictionary<string, MemoryStream>();
        }
        
        public override void PutCompressed(string fileID, MemoryStream stream)
        {
            lock (padlock) {
                streamContainer.Remove(fileID);
                streamContainer.Add(fileID, stream);
            }
        }
        
        public override MemoryStream GetCompressed(string fileID)
        {
            MemoryStream stream = null;
            lock (padlock) {
                streamContainer.TryGetValue(fileID, out stream);
            }
            return stream;
        }
        
        public override void ReturnStream(MemoryStream stream)
        {
            // No-op - we never free this stream because the stream is itself the thing that is holding the data in-memory
        }
    }
}