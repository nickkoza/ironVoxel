// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel.Asynchronous {
    public abstract class QueuedFileChange {    
        protected string filePath;
        protected object contextObject;
        
        public abstract void Apply(FileDatabase[] fileDatabases);
    }
}