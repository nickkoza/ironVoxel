// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel.Asynchronous {
    public class QueuedFileSave : QueuedFileChange {
        public delegate void SaveFinishedCallback(object contextObject);

        protected SaveFinishedCallback saveFinishedCallback;
        protected MemoryStream stream;

        public QueuedFileSave (string filePath, MemoryStream stream, SaveFinishedCallback callback, object context)
        {
            this.filePath = filePath;
            this.stream = cloneStream(stream);
            this.saveFinishedCallback = callback;
            this.contextObject = context;
        }
        
        public override void Apply(FileDatabase[] fileDatabases)
        {
            if (fileDatabases.Length == 0) {
                Debug.LogWarning("QueuedFileLoad::Apply provided with null argument(s).");
                return;
            }

            for (int i = 0; i < fileDatabases.Length; i++) {
                fileDatabases[i].Put(filePath, stream);
            }

            saveFinishedCallback(contextObject);
        }
        
        private MemoryStream cloneStream(MemoryStream stream)
        {
            MemoryStream clonedStream = new MemoryStream();
            stream.Seek(0, SeekOrigin.Begin);
            clonedStream.SetLength(stream.Length);
            while (stream.Position < stream.Length) {
                clonedStream.WriteByte((byte)stream.ReadByte());
            }
            return clonedStream;
        }
    }
}