// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using ironVoxel;
using ironVoxel.Domain;
using ironVoxel.Gameplay;
using ironVoxel.Service;
using ironVoxel.Asynchronous;

namespace ironVoxel.Render {
    public enum RendererType {
        Solid,
        Transparent,
        Water
    }
    
    public sealed class ChunkMesh : MonoBehaviour {
        private float blockMeshHalfSize;
        public Mesh[] meshes;
        public GameObject[] rendererGameObjects;
        public int activeMesh;
        public ChunkMeshCluster associatedChunkMeshCluster;
        private BatchProcessor.WorkFunction generateMeshWorkFunction;
        private Chunk northChunk;
        private Chunk southChunk;
        private Chunk westChunk;
        private Chunk eastChunk;
        private Chunk aboveChunk;
        private Chunk belowChunk;
        private uint meshArraysIndex;
        private List<Vector3> verticesList;
        private List<Vector2> uvList;
        private List<int> trianglesList;
        private List<Vector3> normalsList;
        private List<Color32> colorList;
        RendererType rendererType;
        
        enum MeshGenerationState {
            Waiting,
            SettingVertices,
            SettingTriangles,
            SettingUVs,
            SettingNormals,
            Optimize,
            Done
        }
        
        MeshGenerationState meshGenerationState;
    
        // Use this for initialization
        void Awake()
        {
            associatedChunkMeshCluster = null;
            generateMeshWorkFunction = new BatchProcessor.WorkFunction(GenerateMeshThread);
            meshGenerationState = MeshGenerationState.Waiting;

            verticesList = new List<Vector3>();
            uvList = new List<Vector2>();
            trianglesList = new List<int>();
            normalsList = new List<Vector3>();
            colorList = new List<Color32>();
            
            rendererType = RendererType.Solid;
            
            // Load up our mesh double buffering
            activeMesh = 0;
            meshes = new Mesh[2];
            rendererGameObjects = new GameObject[2];
            int index = 0;
            foreach (Transform child in transform) {
                rendererGameObjects[index] = child.gameObject;
                rendererGameObjects[index].SetActive(false);
                meshes[index] = child.gameObject.GetComponent<MeshFilter>().mesh;
                meshes[index].Clear();
                index++;
            }
        }
        
        public void Setup(ChunkMeshCluster chunkMeshCluster, RendererType rendererType)
        {
            SetRendererType(rendererType);
            if (rendererType == RendererType.Water) {
                // Water rendering requires that we overlap the meshes less, because of the transparency
                blockMeshHalfSize = 0.5f + 0.00001f;
            }
            else {
                // The fractional part seals the mesh and prevents flickering
                blockMeshHalfSize = 0.5f + 0.0005f;
            }
            
            if (chunkMeshCluster != null && chunkMeshCluster.chunk != null) {
                Vector3 position;
                position.x = chunkMeshCluster.chunk.WorldPosition().x * Chunk.SIZE - 0.5f;
                position.y = chunkMeshCluster.chunk.WorldPosition().y * Chunk.SIZE - 0.5f;
                position.z = chunkMeshCluster.chunk.WorldPosition().z * Chunk.SIZE - 0.5f;
                transform.position = position;
            }
            associatedChunkMeshCluster = chunkMeshCluster;
            SetName();
        }
        
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        
        private void SetName()
        {
            if (associatedChunkMeshCluster != null && associatedChunkMeshCluster.chunk != null) {
                gameObject.name = string.Format("Chunk ({0}, {1}, {2} - {3})",
                    associatedChunkMeshCluster.chunk.WorldPosition().x.ToString(),
                    associatedChunkMeshCluster.chunk.WorldPosition().y.ToString(),
                    associatedChunkMeshCluster.chunk.WorldPosition().z.ToString(),
                    rendererType.ToString());
            }
            else {
                ClearName();
            }
        }
        
        public void ClearName()
        {
            gameObject.name = "Chunk (available)";
        }
        
        public void ClearMeshes()
        {
            meshes[0].Clear();
            meshes[1].Clear();
        }
        
        private void SetRendererType(RendererType rendererType)
        {
            this.rendererType = rendererType;
        }
        
        public void Generate(Chunk northChunk, Chunk southChunk,
                                Chunk westChunk, Chunk eastChunk,
                                Chunk aboveChunk, Chunk belowChunk)
        {
            this.northChunk = northChunk;
            this.southChunk = southChunk;
            this.westChunk = westChunk;
            this.eastChunk = eastChunk;
            this.aboveChunk = aboveChunk;
            this.belowChunk = belowChunk;
            AsyncService.GetCPUMediator().EnqueueBatchForProcessing(generateMeshWorkFunction, (object)this,
                                                   CPUMediator.HIGH_PRIORITY, transform.position);
        }
        
        private void GenerateMeshThread(object chunkMeshInstance)
        {
            ChunkMesh chunkMesh = chunkMeshInstance as ChunkMesh;
            chunkMesh.CreateFaces();
            associatedChunkMeshCluster.SignalGenerationComplete(rendererType);
        }
        
        public void IterateOnFinishishingMeshGeneration()
        {
            if (meshGenerationState == MeshGenerationState.Waiting) {
                meshGenerationState = MeshGenerationState.SettingVertices;
            }
            
            if (verticesList.Count > 0) {
                Mesh mesh = GetInactiveMesh();
                if (meshGenerationState == MeshGenerationState.SettingVertices) {
                    mesh.vertices = verticesList.ToArray();
                    mesh.colors32 = colorList.ToArray();
                    int inactiveMesh = (activeMesh == 0 ? 1 : 0);
                    SetRendererToCurrentRendererType(inactiveMesh);
                    meshGenerationState = MeshGenerationState.SettingTriangles;
                }
                else if (meshGenerationState == MeshGenerationState.SettingTriangles) {
                        mesh.triangles = trianglesList.ToArray();
                        meshGenerationState = MeshGenerationState.SettingUVs;
                    }
                    else if (meshGenerationState == MeshGenerationState.SettingUVs) {
                            mesh.uv = uvList.ToArray();
                            meshGenerationState = MeshGenerationState.SettingNormals;
                        }
                        else if (meshGenerationState == MeshGenerationState.SettingNormals) {
                                mesh.normals = normalsList.ToArray();
                                meshGenerationState = MeshGenerationState.Optimize;
                            }
                            else if (meshGenerationState == MeshGenerationState.Optimize) {
                                    mesh.RecalculateBounds();
                                    mesh.Optimize();
                                    mesh.UploadMeshData(false);
                                    meshGenerationState = MeshGenerationState.Done;
                                }
            }
            else {
                meshGenerationState = MeshGenerationState.Done;
            }
            
            if (meshGenerationState == MeshGenerationState.Done) {
                associatedChunkMeshCluster.SignalFinished(rendererType);
                meshGenerationState = MeshGenerationState.Waiting;
                
                FlipBuffers();
            }
        }
        
        private Mesh GetActiveMesh()
        {
            return meshes[activeMesh];
        }
        
        private Mesh GetInactiveMesh()
        {
            return meshes[activeMesh == 0 ? 1 : 0];
        }
        
        private GameObject GetActiveRendererGameObject()
        {
            return rendererGameObjects[activeMesh];
        }
        
        private GameObject GetInactiveRendererGameObject()
        {
            return rendererGameObjects[activeMesh == 0 ? 1 : 0];
        }
        
        private void FlipBuffers()
        {
            GetInactiveRendererGameObject().SetActive(true);
            GetActiveRendererGameObject().SetActive(false);
            GetActiveMesh().Clear();
            activeMesh = (activeMesh == 0 ? 1 : 0);
        }
        
        private void SetRendererToCurrentRendererType(int meshIndex)
        {
            if (rendererType == RendererType.Water) {
                rendererGameObjects[meshIndex].renderer.material = World.Instance().getWaterBlockMaterial();
            }
            else if (rendererType == RendererType.Transparent) {
                rendererGameObjects[meshIndex].renderer.material = World.Instance().getTransparentBlockMaterial();
            }
            else {
                rendererGameObjects[meshIndex].renderer.material = World.Instance().getSolidBlockMaterial();
            }
        }
        
        private bool BlockIsRenderable(Block block)
        {
            return block.IsActive() && (
                (rendererType == RendererType.Solid && block.IsNotTransparent()) ||
                (rendererType == RendererType.Transparent && block.IsTransparent() && block.IsWater() == false) ||
                (rendererType == RendererType.Water && block.IsWater())
            );
        }
        
        /// <summary>
        /// Clears the current mesh and recalculates it from scratch.
        /// Does all required culling based on the chunks passed into it.
        /// Does not create the actual mesh yet; this only calculates it.
        /// </summary>
        private void CreateFaces()
        {
            List<BlockLight> lightList = RenderService.GetAllLightsWithinMaxRange(
                associatedChunkMeshCluster.chunk.WorldPosition());

            // Resist the urge to remove the lists and convert this all to a two-step system of counting the faces and
            // then initializing the arrays to the proper size. Adjacent chunks can change while this calculation is
            // happening, which can result in mis-counts.
            verticesList.Clear();
            uvList.Clear();
            trianglesList.Clear();
            normalsList.Clear();
            colorList.Clear();

            meshArraysIndex = 0;
            for (byte xIttr = 0; xIttr < Chunk.SIZE; xIttr++) {
                for (byte zIttr = 0; zIttr < Chunk.SIZE; zIttr++) {
                    for (int yIttr = Chunk.SIZE - 1; yIttr >= 0; yIttr--) {
                        CreateFacesAtPosition(xIttr, yIttr, zIttr, lightList);
                    }
                }
            }
        }
        
        private bool CreateFacesAtPosition(int x, int y, int z, List<BlockLight> lightList)
        {
            Block block = associatedChunkMeshCluster.chunk.GetBlock(x, y, z);
            if (BlockIsRenderable(block)) {
                Vector3 centerPosition;
                centerPosition.x = x;
                centerPosition.y = y;
                centerPosition.z = z;
                
                bool aboveBlocked, belowBlocked, westBlocked, eastBlocked, northBlocked, southBlocked;              
                if (this.rendererType == RendererType.Solid) {
                    westBlocked = block.AdjacentBlockIsTransparent(-1, 0, 0);
                    eastBlocked = block.AdjacentBlockIsTransparent(1, 0, 0);
                    northBlocked = block.AdjacentBlockIsTransparent(0, 0, -1);
                    southBlocked = block.AdjacentBlockIsTransparent(0, 0, 1);
                    aboveBlocked = block.AdjacentBlockIsTransparent(0, 1, 0);
                    belowBlocked = block.AdjacentBlockIsTransparent(0, -1, 0);
                }
                else {
                    // Transparent render types have more complicated hidden surface removal algorithms, so we can't
                    // use the cached data.
                    CheckSurroundingBlocks(x, y, z, block,
                        out aboveBlocked, out belowBlocked, out westBlocked,
                        out eastBlocked, out northBlocked, out southBlocked);
                }
                
                bool shadeTopLeft, shadeTopRight, shadeBottomLeft, shadeBottomRight;
                shadeTopLeft = false;
                shadeTopRight = false;
                shadeBottomLeft = false;
                shadeBottomRight = false;
                
                if (!westBlocked) {
                    shadeBottomRight = block.AdjacentBlockIsTransparent(-1, -1, 0) ||
                        block.AdjacentBlockIsTransparent(-1, 0, -1) ||
                        block.AdjacentBlockIsTransparent(-1, -1, -1);
                    
                    shadeBottomLeft = block.AdjacentBlockIsTransparent(-1, -1, 0) ||
                        block.AdjacentBlockIsTransparent(-1, 0, 1) ||
                        block.AdjacentBlockIsTransparent(-1, -1, 1);
                    
                    shadeTopRight = block.AdjacentBlockIsTransparent(-1, 1, 0) ||
                        block.AdjacentBlockIsTransparent(-1, 0, -1) ||
                        block.AdjacentBlockIsTransparent(-1, 1, -1);
                    
                    shadeTopLeft = block.AdjacentBlockIsTransparent(-1, 1, 0) ||
                        block.AdjacentBlockIsTransparent(-1, 0, 1) ||
                        block.AdjacentBlockIsTransparent(-1, 1, 1);
                    
                    AddFace(centerPosition, CubeSide.West, block, aboveBlocked, belowBlocked, lightList,
                        shadeTopLeft, shadeTopRight, shadeBottomLeft, shadeBottomRight);
                    
                    shadeTopLeft = false;
                    shadeTopRight = false;
                    shadeBottomLeft = false;
                    shadeBottomRight = false;
                }
                
                if (!eastBlocked) {
                    shadeBottomLeft = block.AdjacentBlockIsTransparent(1, -1, 0) ||
                        block.AdjacentBlockIsTransparent(1, 0, -1) ||
                        block.AdjacentBlockIsTransparent(1, -1, -1);
                    
                    shadeBottomRight = block.AdjacentBlockIsTransparent(1, -1, 0) ||
                        block.AdjacentBlockIsTransparent(1, 0, 1) ||
                        block.AdjacentBlockIsTransparent(1, -1, 1);
                    
                    shadeTopLeft = block.AdjacentBlockIsTransparent(1, 1, 0) ||
                        block.AdjacentBlockIsTransparent(1, 0, -1) ||
                        block.AdjacentBlockIsTransparent(1, 1, -1);
                    
                    shadeTopRight = block.AdjacentBlockIsTransparent(1, 1, 0) ||
                        block.AdjacentBlockIsTransparent(1, 0, 1) ||
                        block.AdjacentBlockIsTransparent(1, 1, 1);
                    
                    AddFace(centerPosition, CubeSide.East, block, aboveBlocked, belowBlocked, lightList,
                        shadeTopLeft, shadeTopRight, shadeBottomLeft, shadeBottomRight);
                    
                    shadeTopLeft = false;
                    shadeTopRight = false;
                    shadeBottomLeft = false;
                    shadeBottomRight = false;
                }
                
                if (!belowBlocked) {
                    shadeBottomRight = block.AdjacentBlockIsTransparent(0, -1, -1) ||
                        block.AdjacentBlockIsTransparent(-1, -1, 0) ||
                        block.AdjacentBlockIsTransparent(-1, -1, -1);
                    
                    shadeBottomLeft = block.AdjacentBlockIsTransparent(0, -1, -1) ||
                        block.AdjacentBlockIsTransparent(1, -1, 0) ||
                        block.AdjacentBlockIsTransparent(1, -1, -1);
                    
                    shadeTopRight = block.AdjacentBlockIsTransparent(0, -1, 1) ||
                        block.AdjacentBlockIsTransparent(-1, -1, 0) ||
                        block.AdjacentBlockIsTransparent(-1, -1, 1);
                    
                    shadeTopLeft = block.AdjacentBlockIsTransparent(0, -1, 1) ||
                        block.AdjacentBlockIsTransparent(1, -1, 0) ||
                        block.AdjacentBlockIsTransparent(1, -1, 1);
                    
                    AddFace(centerPosition, CubeSide.Bottom, block, aboveBlocked, belowBlocked, lightList,
                        shadeTopLeft, shadeTopRight, shadeBottomLeft, shadeBottomRight);
                    
                    shadeTopLeft = false;
                    shadeTopRight = false;
                    shadeBottomLeft = false;
                    shadeBottomRight = false;
                }
                
                if (!aboveBlocked) {
                    shadeBottomLeft = block.AdjacentBlockIsTransparent(0, 1, -1) ||
                        block.AdjacentBlockIsTransparent(-1, 1, 0) ||
                        block.AdjacentBlockIsTransparent(-1, 1, -1);
                    
                    shadeBottomRight = block.AdjacentBlockIsTransparent(0, 1, -1) ||
                        block.AdjacentBlockIsTransparent(1, 1, 0) ||
                        block.AdjacentBlockIsTransparent(1, 1, -1);
                    
                    shadeTopLeft = block.AdjacentBlockIsTransparent(0, 1, 1) ||
                        block.AdjacentBlockIsTransparent(-1, 1, 0) ||
                        block.AdjacentBlockIsTransparent(-1, 1, 1);
                    
                    shadeTopRight = block.AdjacentBlockIsTransparent(0, 1, 1) ||
                        block.AdjacentBlockIsTransparent(1, 1, 0) ||
                        block.AdjacentBlockIsTransparent(1, 1, 1);
                    
                    AddFace(centerPosition, CubeSide.Top, block, aboveBlocked, belowBlocked, lightList,
                        shadeTopLeft, shadeTopRight, shadeBottomLeft, shadeBottomRight);
                    shadeTopLeft = false;
                    shadeTopRight = false;
                    shadeBottomLeft = false;
                    shadeBottomRight = false;
                }
                
                if (!northBlocked) {
                    shadeBottomLeft = block.AdjacentBlockIsTransparent(0, -1, -1) ||
                        block.AdjacentBlockIsTransparent(-1, 0, -1) ||
                        block.AdjacentBlockIsTransparent(-1, -1, -1);
                    
                    shadeBottomRight = block.AdjacentBlockIsTransparent(0, -1, -1) ||
                        block.AdjacentBlockIsTransparent(1, 0, -1) ||
                        block.AdjacentBlockIsTransparent(1, -1, -1);
                    
                    shadeTopLeft = block.AdjacentBlockIsTransparent(0, 1, -1) ||
                        block.AdjacentBlockIsTransparent(-1, 0, -1) ||
                        block.AdjacentBlockIsTransparent(-1, 1, -1);
                    
                    shadeTopRight = block.AdjacentBlockIsTransparent(0, 1, -1) ||
                        block.AdjacentBlockIsTransparent(1, 0, -1) ||
                        block.AdjacentBlockIsTransparent(1, 1, -1);
                    
                    AddFace(centerPosition, CubeSide.North, block, aboveBlocked, belowBlocked, lightList,
                        shadeTopLeft, shadeTopRight, shadeBottomLeft, shadeBottomRight);
                    
                    shadeTopLeft = false;
                    shadeTopRight = false;
                    shadeBottomLeft = false;
                    shadeBottomRight = false;
                }
                
                if (!southBlocked) {
                    shadeBottomRight = block.AdjacentBlockIsTransparent(0, -1, 1) ||
                        block.AdjacentBlockIsTransparent(-1, 0, 1) ||
                        block.AdjacentBlockIsTransparent(-1, -1, 1);
                    
                    shadeBottomLeft = block.AdjacentBlockIsTransparent(0, -1, 1) ||
                        block.AdjacentBlockIsTransparent(1, 0, 1) ||
                        block.AdjacentBlockIsTransparent(1, -1, 1);
                    
                    shadeTopRight = block.AdjacentBlockIsTransparent(0, 1, 1) ||
                        block.AdjacentBlockIsTransparent(-1, 0, 1) ||
                        block.AdjacentBlockIsTransparent(-1, 1, 1);
                    
                    shadeTopLeft = block.AdjacentBlockIsTransparent(0, 1, 1) ||
                        block.AdjacentBlockIsTransparent(1, 0, 1) ||
                        block.AdjacentBlockIsTransparent(1, 1, 1);
                    
                    AddFace(centerPosition, CubeSide.South, block, aboveBlocked, belowBlocked, lightList,
                        shadeTopLeft, shadeTopRight, shadeBottomLeft, shadeBottomRight);
                    
                    shadeTopLeft = false;
                    shadeTopRight = false;
                    shadeBottomLeft = false;
                    shadeBottomRight = false;
                }
                
                return true;
            }
            else {
                return false;
            }
        }
        
        
        /// <summary>
        /// Checks all sides of a block and returns whether each side's view is blocked.
        /// </summary>
        private bool CheckSurroundingBlocks(int xIttr, int yIttr, int zIttr, Block block,
            out bool aboveBlocked, out bool belowBlocked, out bool westBlocked,
            out bool eastBlocked, out bool northBlocked, out bool southBlocked)
        {
            aboveBlocked = belowBlocked = westBlocked = eastBlocked = northBlocked = southBlocked = true;
            
            if (associatedChunkMeshCluster == null) {
                return false;
            }
    
            // West
            if (xIttr == 0) {
                if (westChunk != null && westChunk.GetBlock(Chunk.SIZE - 1, yIttr, zIttr).DoesNotBlockViewOf(block)) {
                    westBlocked = false;
                }
            }
            else if (associatedChunkMeshCluster.chunk.GetBlock(xIttr - 1, yIttr, zIttr).DoesNotBlockViewOf(block)) {
                    westBlocked = false;
                }
            
            // East
            if (xIttr == Chunk.SIZE - 1) {
                if (eastChunk != null && eastChunk.GetBlock(0, yIttr, zIttr).DoesNotBlockViewOf(block)) {
                    eastBlocked = false;
                }
            }
            else if (associatedChunkMeshCluster.chunk.GetBlock(xIttr + 1, yIttr, zIttr).DoesNotBlockViewOf(block)) {
                    eastBlocked = false;
                }
            
            // Bottom
            if (yIttr == 0) {
                if (belowChunk != null && belowChunk.GetBlock(xIttr, Chunk.SIZE - 1, zIttr).DoesNotBlockViewOf(block)) {
                    belowBlocked = false;
                }
            }
            else if (associatedChunkMeshCluster.chunk.GetBlock(xIttr, yIttr - 1, zIttr).DoesNotBlockViewOf(block)) {
                    belowBlocked = false;
                }
            
            // Top
            if (yIttr == Chunk.SIZE - 1) {
                if (aboveChunk != null && aboveChunk.GetBlock(xIttr, 0, zIttr).DoesNotBlockViewOf(block)) {
                    aboveBlocked = false;
                }
            }
            else if (associatedChunkMeshCluster.chunk.GetBlock(xIttr, yIttr + 1, zIttr).DoesNotBlockViewOf(block)) {
                    aboveBlocked = false;
                }
            
            // North
            if (zIttr == 0) {
                if (northChunk != null && northChunk.GetBlock(xIttr, yIttr, Chunk.SIZE - 1).DoesNotBlockViewOf(block)) {
                    northBlocked = false;
                }
            }
            else if (associatedChunkMeshCluster.chunk.GetBlock(xIttr, yIttr, zIttr - 1).DoesNotBlockViewOf(block)) {
                    northBlocked = false;
                }
            
            // South
            if (zIttr == Chunk.SIZE - 1) {
                if (southChunk != null && southChunk.GetBlock(xIttr, yIttr, 0).DoesNotBlockViewOf(block)) {
                    southBlocked = false;
                }
            }
            else if (associatedChunkMeshCluster.chunk.GetBlock(xIttr, yIttr, zIttr + 1).DoesNotBlockViewOf(block)) {
                    southBlocked = false;
                }
            
            return true;
        }
        
        /// <summary>
        /// Get a single face for the mesh setting.
        /// </summary>
        /// <param name='centerPosition'>
        /// Where the center of the block resides.
        /// </param>
        /// <param name='side'>
        /// Which side of the cube to add.
        /// </param>
        /// <param name='block'>
        /// The block that the face belongs to.
        /// </param>
        /// <param name='blockAbove'>
        /// Whether there is a block above the one being represented by the face.
        /// </param>
        /// <param name='blockBelow'>
        /// Whether there is a block below the one being represented by the face.
        /// </param>
        /// <param name='hasSunlight'>
        /// Whether sunlight is reaching the block.
        /// </param>
        private void AddFace(Vector3 centerPosition, CubeSide side, Block block, bool blockAbove, bool blockBelow,
                            List<BlockLight> lightList,
                            bool shadeTopLeft, bool shadeTopRight, bool shadeBottomLeft, bool shadeBottomRight)
        {
            Vector3 vertice;
            Vector3 normalDirection = Vector3.zero;

            Color lightColor;
            ChunkSubspacePosition subspaceSamplePosition;
            subspaceSamplePosition.x = (int)centerPosition.x;
            subspaceSamplePosition.y = (int)centerPosition.y;
            subspaceSamplePosition.z = (int)centerPosition.z;
            BlockSpacePosition worldSamplePosition =
                subspaceSamplePosition.GetBlockSpacePosition(associatedChunkMeshCluster.chunk);
            lightColor = RenderService.SampleLight(lightList, worldSamplePosition, side);

            if (side == CubeSide.Top) {
                normalDirection = Vector3.up;
                
                // 0, 1, 0
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);
                
                // 0, 1, 1
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);
                
                // 1, 1, 0
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);
                
                // 1, 1, 1
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);
            }
            else if (side == CubeSide.Bottom) {
                normalDirection = Vector3.down;
            
                // 1, 0, 0
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);
            
                // 1, 0, 1
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);
            
                // 0, 0, 0
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);
            
                // 0, 0, 1
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);
            }
            else if (side == CubeSide.North) {
                normalDirection = Vector3.back;
        
                // 0, 0, 0
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);
        
                // 0, 1, 0
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);
        
                // 1, 0, 0
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);
        
                // 1, 1, 0
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);
            }
            else if (side == CubeSide.South) {
                normalDirection = Vector3.forward;
    
                // 1, 0, 1
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);
    
                // 1, 1, 1
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);
    
                // 0, 0, 1
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);
    
                // 0, 1, 1
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);
            }
            else if (side == CubeSide.West) {
                normalDirection = Vector3.left;

                // 0, 0, 1
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);

                // 0, 1, 1
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);

                // 0, 0, 0
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);

                // 0, 1, 0
                vertice.x = centerPosition.x - blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);
            }
            else if (side == CubeSide.East) {
                normalDirection = Vector3.right;

                // 1, 0, 0
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);

                // 1, 1, 0
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z - blockMeshHalfSize;
                verticesList.Add(vertice);

                // 1, 0, 1
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y - blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);

                // 1, 1, 1
                vertice.x = centerPosition.x + blockMeshHalfSize;
                vertice.y = centerPosition.y + blockMeshHalfSize;
                vertice.z = centerPosition.z + blockMeshHalfSize;
                verticesList.Add(vertice);
            }

            trianglesList.Add((int)(meshArraysIndex * 4 + 0));
            trianglesList.Add((int)(meshArraysIndex * 4 + 1));
            trianglesList.Add((int)(meshArraysIndex * 4 + 2));
            trianglesList.Add((int)(meshArraysIndex * 4 + 1));
            trianglesList.Add((int)(meshArraysIndex * 4 + 3));
            trianglesList.Add((int)(meshArraysIndex * 4 + 2));
            
            
            // Determine whether this block is exposed directly to sunlight (nothing at all above it)
            byte sunlightValue = 255;
            BlockSpacePosition checkPosition = worldSamplePosition;
            while (checkPosition.y < Configuration.HEIGHT) {
                checkPosition.y++;
                Block checkBlock = ChunkRepository.GetBlockAtPosition(checkPosition);
                if (checkBlock.IsNotTransparent() && checkBlock.IsActive()) {
                    sunlightValue = 0;
                    break;
                }
            }
            
            Vector2 textureCoordinates = block.GetTextureCoordinates(side, blockAbove, blockBelow, sunlightValue);
            Vector2 overallTextureSize = block.GetOverallTextureSize();
            Vector2 individualTextureSize = block.GetIndividualTextureSize();
            
            Vector2 lowerUVs, upperUVs;
            lowerUVs.x = textureCoordinates.x / overallTextureSize.x;
            lowerUVs.y = 1.0f - textureCoordinates.y / overallTextureSize.y;
            upperUVs.x = (textureCoordinates.x + individualTextureSize.x) / overallTextureSize.x;
            upperUVs.y = 1.0f - (textureCoordinates.y + individualTextureSize.y) / overallTextureSize.y;
            
            Vector2 uv;
            uv.x = lowerUVs.x;
            uv.y = upperUVs.y;
            uvList.Add(uv);
            uv.x = lowerUVs.x;
            uv.y = lowerUVs.y;
            uvList.Add(uv);
            uv.x = upperUVs.x;
            uv.y = upperUVs.y;
            uvList.Add(uv);
            uv.x = upperUVs.x;
            uv.y = lowerUVs.y;
            uvList.Add(uv);

            normalsList.Add(normalDirection);
            normalsList.Add(normalDirection);
            normalsList.Add(normalDirection);
            normalsList.Add(normalDirection);

            Color32 color;
            color.a = 255;
            color.r = (byte)(lightColor.r * 255);
            color.g = (byte)(lightColor.g * 255);
            color.b = (byte)(lightColor.b * 255);

            Color32 shadedColor;
            if (shadeTopLeft || shadeTopRight || shadeBottomLeft || shadeBottomRight) {
                HSBColor hsbShadedColor = HSBColor.FromColor(lightColor);
                hsbShadedColor.b = Mathf.Max(hsbShadedColor.b - 0.37f, 0.063f);
                shadedColor = hsbShadedColor.ToColor();
            }
            else {
                shadedColor = color;
            }

            colorList.Add(shadeBottomLeft ? shadedColor : color);
            colorList.Add(shadeTopLeft ? shadedColor : color);
            colorList.Add(shadeBottomRight ? shadedColor : color);
            colorList.Add(shadeTopRight ? shadedColor : color);

            meshArraysIndex++;
        }
    }
}
