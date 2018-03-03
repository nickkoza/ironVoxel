// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

#define SHOW_DEBUG_UI
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using ironVoxel;
using ironVoxel.Gameplay;
using ironVoxel.Service;
using ironVoxel.Domain;
using ironVoxel.Asynchronous;

namespace ironVoxel {
    public sealed class World : MonoBehaviour {
        private int seed;
        private float fogDistance;
        private bool showDebugMenu;
        private static World instance;
        public BlockParticle blockParticle;
        public Player player;
        public Material solidBlockMaterial;
        public Material transparentBlockMaterial;
        public Material waterBlockMaterial;
        public UnityEngine.Object chunkMeshPrefab;

        public static World Instance()
        {
            return instance;
        }

        void Start()
        {
            if (player == null) {
                throw new MissingComponentException("Missing Player component");
            }
            if (blockParticle == null) {
                throw new MissingComponentException("Missing Block Particle component");
            }
            if (chunkMeshPrefab == null) {
                throw new MissingComponentException("Missing Chunk Mesh component");
            }
            if (solidBlockMaterial == null) {
                throw new MissingComponentException("Missing Solid Block Material component");
            }
            if (transparentBlockMaterial == null) {
                throw new MissingComponentException("Missing Transparent Block Material component");
            }
            if (waterBlockMaterial == null) {
                throw new MissingComponentException("Missing Water Block Material component");
            }

            Vector3 playerStartPosition;
            playerStartPosition.x = 64.5f;
            playerStartPosition.y = 148;
            playerStartPosition.z = 64.5f;
            Instantiate(player, playerStartPosition, Quaternion.identity);

            instance = this;
            BlockDefinition.InitializeAllTypes();
            AsyncService.Initialize();
            ChunkRepository.Initialize();
            CollisionService.Initialize();
            GeneratorService.Initialize();
            RenderService.Initialize();
            
            SetSeed(UnityEngine.Random.Range(Int32.MinValue + 1, Int32.MaxValue - 1));

            fogDistance = 0.0f;

            AsyncService.Load();
            
            // Remove the next line if you want your game to stop processing when it loses focus
            Application.runInBackground = true;
        }

        void Update()
        {
            if (AsyncService.Loading()) {
                return;
            }

            AsyncService.StartFrameTimer();


            if (AsyncService.FrameElapsedPercentageIsNotExceeded(Configuration.PERFORMANCE_GENERATE_NEW_CHUNKS_DEADLINE)) {
                Chunk newChunk = GeneratorService.GenerateNewChunk();
                if (newChunk != null) {
                    ChunkRepository.AddToProcessingChunkList(newChunk);
                }
            }

            int i = 0;
            while (AsyncService.FrameElapsedPercentageIsNotExceeded(Configuration.PERFORMANCE_START_WORK_DEADLINE)
                  && i < ChunkRepository.GetProcessingChunkListSize()) {
                Chunk chunk = ChunkRepository.GetProcessingChunk(i);
                if (chunk == null) {
                    continue;
                }

                if (AsyncService.FrameElapsedPercentageIsNotExceeded(Configuration.PERFORMANCE_FLUSH_MODIFICATIONS_DEADLINE)) {
                    chunk.FlushModifications();
                }

                if (AsyncService.FrameElapsedPercentageIsNotExceeded(Configuration.PERFORMANCE_FINISH_MESH_DEADLINE)) {
                    RenderService.FinishMeshGeneration(chunk);
                }

                if (AsyncService.FrameElapsedPercentageIsNotExceeded(Configuration.PERFORMANCE_GENERATE_MESH_DEADLINE) &&
                    AsyncService.ThreadQueueSize() < Configuration.PERFORMANCE_MAX_THREAD_QUEUE_SIZE) {
                    
                    RenderService.GenerateMeshes(chunk); // Threaded
                }

                if (AsyncService.FrameElapsedPercentageIsNotExceeded(Configuration.PERFORMANCE_MARK_CHUNKS_FOR_MESH_UPDATE_DEADLINE)) {
                    RenderService.MarkSurroundingChunksForMeshUpdate(chunk);
                }

                if (AsyncService.FrameElapsedPercentageIsNotExceeded(Configuration.PERFORMANCE_GENERATE_BLOCKS_DEADLINE) &&
                    AsyncService.ThreadQueueSize() < Configuration.PERFORMANCE_MAX_THREAD_QUEUE_SIZE) {
                    
                    chunk.GenerateBlocks(GetSeed()); // Threaded
                }

                if (chunk.IsFinishedProcessing()) {
                    ChunkRepository.RemoveFromProcessingChunkList(chunk);
                }

                i++;
            }

            GeneratorService.UnloadDeadChunks();
            RenderService.CullChunks();

            int numberOfChunks = ChunkRepository.NumberOfChunks();
            for (int chunkIndex = 0; chunkIndex < numberOfChunks; chunkIndex++) {
                Chunk chunk = ChunkRepository.GetChunkAtIndex(chunkIndex);
                if (chunk.IsFinishedProcessing()) {
                    Vector3 position;
                    position.x = chunk.WorldPosition().x * Chunk.SIZE;
                    position.y = chunk.WorldPosition().y * Chunk.SIZE;
                    position.z = chunk.WorldPosition().z * Chunk.SIZE;

                    float distance = Vector3.Distance(position, Camera.main.transform.position);
                    if (distance > fogDistance) {
                        fogDistance = distance;
                    }
                }
            }
            SendMessage("UpdateFogDistance", fogDistance, SendMessageOptions.DontRequireReceiver);

            GeneratorService.CleanupOldChunks();
            AsyncService.RePrioritizeCPUMediatorWork();
            ChunkRepository.RePrioritizeSortProcessingChunkList();
            ChunkRepository.FlushProcessingChunkListModifications();

            if (Input.GetKeyDown(KeyCode.F12)) {
                showDebugMenu = !showDebugMenu;
            }

            if (Input.GetKeyDown(KeyCode.F5)) {
                SaveScreenshot();
            }

            if (Input.GetKeyDown(KeyCode.F9)) {
                ChunkRepository.DumpProcessingChunkListDebugData();
            }

            if (Input.GetKeyDown(KeyCode.Escape)) {
                Application.Quit();
            }
        }

        void OnApplicationQuit()
        {
            AsyncService.GetCPUMediator().Shutdown();

            UnityEngine.Debug.Log("Saving all loaded chunks...");
            ChunkRepository.SaveAllLoadedChunks();
            FileRepository.Shutdown();
            UnityEngine.Debug.Log("Done.");
        }
        
        // -----------------------------------------------------------------------------------------------------------------
        // START PUBLIC API

        public int GetSeed()
        {
            return seed;
        }
        
        public void SetSeed(int seed)
        {
            this.seed = seed;
            PerlinNoise.SetSeed(seed);
            UnityEngine.Debug.Log(String.Format("World seed: {0}", seed));
        }

        private int screenshotCounter = 1;

        private void SaveScreenshot()
        {
            string screenshotFilename, screenshotPath, directory;
            do {
                screenshotFilename = String.Format("ironVoxelScreenshot_{0:D4}.png", screenshotCounter);
                directory = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots");
                screenshotPath = System.IO.Path.Combine(directory, screenshotFilename);
                screenshotCounter++;
            }
            while(System.IO.File.Exists(screenshotFilename));

            if (!System.IO.Directory.Exists(directory)) {
                System.IO.Directory.CreateDirectory(directory);
            }
            Application.CaptureScreenshot(screenshotFilename);
            UnityEngine.Debug.Log("Screenshot saved to: " + screenshotPath);
        }

        public Material getSolidBlockMaterial() {
            return solidBlockMaterial;
        }

        public Material getTransparentBlockMaterial() {
            return transparentBlockMaterial;
        }

        public Material getWaterBlockMaterial() {
            return waterBlockMaterial;
        }

        public UnityEngine.Object getChunkMeshPrefab() {
            return chunkMeshPrefab;
        }

        // -----------------------------------------------------------------
        // Debug
        static private Rect uiThreadQueueSizeRect = new Rect(0, 20, 200, 25);
        static private Rect uiProcessingListSizeRect = new Rect(0, 45, 200, 25);
        static private Rect uiTotalChunkCountRect = new Rect(0, 70, 200, 25);
        static private Rect uiUnloadChunkCountRect = new Rect(0, 95, 200, 25);
    #if SHOW_DEBUG_UI
        void OnGUI()
        {
            if (showDebugMenu) {
                GUI.Box(uiThreadQueueSizeRect, "Thread queue size: " + AsyncService.ThreadQueueSize().ToString());
                GUI.Box(uiProcessingListSizeRect, "Processing chunk queue size: " + ChunkRepository.GetProcessingChunkListSize().ToString());
                GUI.Box(uiTotalChunkCountRect, "Chunk pool size: " + GeneratorService.TotalChunkCount().ToString());
                GUI.Box(uiUnloadChunkCountRect, "Queued chunk unloads: " + GeneratorService.UnloadChunksListCount().ToString());
            }
        }
    #endif
    }
}
