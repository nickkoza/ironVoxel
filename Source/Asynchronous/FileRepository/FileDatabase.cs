// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel.Asynchronous {
    public abstract class FileDatabase {
        public abstract void PutCompressed(string fileID, MemoryStream stream);

        public abstract MemoryStream GetCompressed(string fileID);

        public abstract void ReturnStream(MemoryStream stream);
        
        public virtual void Put(string fileID, MemoryStream stream)
        {
            MemoryStream compressedStream = Compress(stream);
            if (compressedStream != null) {
                PutCompressed(fileID, compressedStream);
            }
        }
        
        public virtual MemoryStream Get(string fileID)
        {
            MemoryStream stream = Inflate(GetCompressed(fileID));
            if (stream == null) {
                return null;
            }
            else {
                return stream;
            }
        }

        // Compression and inflation do not currently work, because Unity does not support C#'s System.IO.Compression. I'm leaving this code in here to give
        // you an idea of how to compress/inflate the stream on the fly, if you'd like to use another compression library.
        #if UNITY_ADDED_GZIP_SUPPORT
        private MemoryStream Compress(MemoryStream stream)
        {
            GZipStream compressionStream = Copy(stream, CompressionMode.Compress);
            MemoryStream dest = Copy(compressionStream);
            return dest;
        }

        private MemoryStream Inflate(MemoryStream stream)
        {
            if (stream == null) {
                return null;
            }

            GZipStream inflationStream = Copy(stream, CompressionMode.Decompress);
            MemoryStream dest = Copy(inflationStream);
            return dest;
        }
        
        private MemoryStream Copy(GZipStream origin)
        {
            MemoryStream dest;
            int length = (int)origin.Length;
            if (length > 0) {
                dest = new MemoryStream();
                byte[] buffer = new byte[length];
                origin.Read(buffer, 0, length);
                dest.Write(buffer, 0, length);
            }
            else {
                dest = null;
            }
            return dest;
        }
        
        private GZipStream Copy(MemoryStream origin, CompressionMode compressionMode)
        {
            GZipStream gzipStream;
            int length = (int)origin.Length;
            if (length > 0) {
                gzipStream = new GZipStream(origin, compressionMode);
                byte[] buffer = new byte[length];
                origin.Read(buffer, 0, length);
                gzipStream.Write(buffer, 0, length);
            }
            else {
                gzipStream = null;
            }
            return gzipStream;
        }
        #else
        private MemoryStream Compress(MemoryStream stream)
        {
            return stream;
        }
        
        private MemoryStream Inflate(MemoryStream stream)
        {
            return stream;
        }
        #endif
    }
}