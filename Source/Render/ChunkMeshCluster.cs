// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System;
using System.Collections;

using ironVoxel.Domain;
using ironVoxel.Pool;

namespace ironVoxel.Render {
    public sealed class ChunkMeshCluster {
        private enum ChunkMeshState {
            New,
            Generating,
            GenerationComplete,
            Finishing,
            Done
        }
        
        int numberOfRenderers;
        ChunkMesh[] chunkMeshes;
        ChunkMeshState[] chunkMeshStates;
        public Chunk chunk;
        
        public ChunkMeshCluster ()
        {
            numberOfRenderers = Enum.GetNames(typeof(RendererType)).Length;
            chunkMeshes = new ChunkMesh[numberOfRenderers];
            chunkMeshStates = new ChunkMeshState[numberOfRenderers];
            
            for (int i = 0; i < numberOfRenderers; i++) {
                chunkMeshes[i] = ChunkMeshPool.Instance().GetChunkMesh(this, (RendererType)i);
                chunkMeshStates[i] = ChunkMeshState.New;
            }
        }
        
        public void Setup(Chunk chunk)
        {
            this.chunk = chunk;
            for (int i = 0; i < numberOfRenderers; i++) {
                chunkMeshes[i].Setup(this, (RendererType)i);
            }
        }
        
        public void Generate(Chunk northChunk, Chunk southChunk,
                                Chunk westChunk, Chunk eastChunk,
                                Chunk aboveChunk, Chunk belowChunk)
        {
            for (int i = 0; i < numberOfRenderers; i++) {
                chunkMeshStates[i] = ChunkMeshState.Generating;
                chunkMeshes[i].Generate(northChunk, southChunk, westChunk, eastChunk, aboveChunk, belowChunk);
            }
        }
        
        public void IterateOnFinishingMeshGeneration()
        {
            for (int i = 0; i < numberOfRenderers; i++) {
                chunkMeshStates[i] = ChunkMeshState.Finishing;
                chunkMeshes[i].IterateOnFinishishingMeshGeneration();
            }
        }
        
        public void Clear()
        {
            for (int i = 0; i < numberOfRenderers; i++) {
                chunkMeshes[i].ClearMeshes();
                chunkMeshes[i].ClearName();
                chunkMeshStates[i] = ChunkMeshState.New;
            }
        }
        
        public void SignalGenerating(RendererType rendererType)
        {
            chunkMeshStates[(int)rendererType] = ChunkMeshState.Generating;
        }
        
        public void SignalGenerationComplete(RendererType rendererType)
        {
            chunkMeshStates[(int)rendererType] = ChunkMeshState.GenerationComplete;
            if (GenerationIsComplete()) {
                chunk.SetLoadState(ChunkLoadState.MeshCalculationComplete);
            }
        }
        
        public void SignalFinishing(RendererType rendererType)
        {
            chunkMeshStates[(int)rendererType] = ChunkMeshState.Finishing;
        }
        
        public void SignalFinished(RendererType rendererType)
        {
            chunkMeshStates[(int)rendererType] = ChunkMeshState.Done;
            if (IsFinished()) {
                SetVisible(chunk.IsVisible());
                chunk.SetLoadState(ChunkLoadState.Done);
            }
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            for (int i = 0; i < numberOfRenderers; i++) {
                chunkMeshes[i].gameObject.SetActive(visible);
            }
        }
        
        public bool IsGenerating()
        {
            return AllChunkMeshesAreAtState(ChunkMeshState.Generating);
        }

        public bool GenerationIsComplete()
        {
            return AllChunkMeshesAreAtState(ChunkMeshState.GenerationComplete);
        }

        public bool IsFinishing()
        {
            return AllChunkMeshesAreAtState(ChunkMeshState.Finishing);
        }

        public bool IsFinished()
        {
            return AllChunkMeshesAreAtState(ChunkMeshState.Done);
        }
        
        private bool AllChunkMeshesAreAtState(ChunkMeshState chunkMeshState)
        {
            bool correct = true;
            for (int i = 0; i < numberOfRenderers; i++) {
                if (chunkMeshStates[i] != chunkMeshState) {
                    correct = false;
                    break;
                }
            }
            return correct;
        }
    }
}
