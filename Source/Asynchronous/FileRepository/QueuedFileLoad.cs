// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel.Asynchronous {
    public class QueuedFileLoad : QueuedFileChange {
        public delegate void LoadFinishedCallback(string filePath, MemoryStream fileStream, object contextObject);

        protected LoadFinishedCallback loadFinishedCallback;
        
        public QueuedFileLoad (string filePath, LoadFinishedCallback callback, object context)
        {
            this.filePath = filePath;
            this.loadFinishedCallback = callback;
            this.contextObject = context;
        }
        
        public override void Apply(FileDatabase[] fileDatabases)
        {
            if (fileDatabases.Length == 0) {
                Debug.LogWarning("QueuedFileLoad::Apply provided with null argument(s).");
                return;
            }

            MemoryStream stream = null;
            for (int i = 0; i < fileDatabases.Length; i++) {
                stream = fileDatabases[i].Get(filePath);
                if (stream != null) {
                    loadFinishedCallback(filePath, stream, contextObject);
                    fileDatabases[i].ReturnStream(stream);
                    return;
                }
            }

            loadFinishedCallback(filePath, null, contextObject);
        }
    }
}