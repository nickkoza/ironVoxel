// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel.Asynchronous {
    public class PhysicalMediaFileDatabase : FileDatabase {
        private string persistentDataPath;

        public PhysicalMediaFileDatabase ()
        {
            persistentDataPath = Application.persistentDataPath;
            Debug.Log("World files path: " + persistentDataPath);
        }
        
        public override void PutCompressed(string fileID, MemoryStream stream)
        {
            FileStream fileStream = File.Create(FilePath(fileID));
            fileStream.Seek(0, SeekOrigin.Begin);
            stream.Seek(0, SeekOrigin.Begin);
            
            long length = stream.Length;
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, (int)length);
            fileStream.Write(buffer, 0, (int)length);
            fileStream.Close();
        }
        
        public override MemoryStream GetCompressed(string fileID)
        {
            if (File.Exists(FilePath(fileID)) == false) { return null; }

            MemoryStream stream = new MemoryStream();
            using (FileStream fileStream = File.OpenRead(FilePath(fileID))) {
                fileStream.Seek(0, SeekOrigin.Begin);
                
                long length = fileStream.Length;
                byte[] buffer = new byte[length];
                fileStream.Read(buffer, 0, (int)length);
                stream.Write(buffer, 0, (int)length);
            }
            return stream;
        }
        
        public override void ReturnStream(MemoryStream stream)
        {
            stream.Dispose();
        }
        
        private string FilePath(string fileID)
        {
            return System.IO.Path.Combine(persistentDataPath, String.Format("{0}.bin", fileID));
        }
    }
}