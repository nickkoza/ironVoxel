// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

using ironVoxel;
using ironVoxel.Gameplay;
using ironVoxel.Domain;

namespace ironVoxel.Gameplay {
    public class Player : MonoBehaviour {
        public GameObject blockSelectorPrefab;
        public GameObject faceSelectorPrefab;
        public GameObject explosiveBallPrefab;
        public Texture2D crosshairTexture;
        private GameObject blockSelector;
        private GameObject faceSelector;
        const int STANDARD_REACH = 5;
        const int DEVELOPER_REACH = 45;
        int reach;
        BlockType buildType;
        int buildTypeIndex;
        bool showUI;
        Rect reticlePosition;

        void Start()
        {
            if (blockSelectorPrefab == null) {
                throw new MissingComponentException("Missing Block Selector component");
            }
            if (faceSelectorPrefab == null) {
                throw new MissingComponentException("Missing Face Selector component");
            }
            if (crosshairTexture == null) {
                throw new MissingComponentException("Missing Crosshair Texture component");
            }
            faceSelector = Instantiate(faceSelectorPrefab, Vector3.zero, Quaternion.identity) as GameObject;
            blockSelector = Instantiate(blockSelectorPrefab, Vector3.zero, Quaternion.identity) as GameObject;

            reach = STANDARD_REACH;

            buildType = BlockType.Dirt;
            buildTypeIndex = 0;
            showUI = true;
            reticlePosition = new Rect(Screen.width / 2 - crosshairTexture.width / 2,
                                        Screen.height / 2 - crosshairTexture.height / 2,
                                        crosshairTexture.width,
                                        crosshairTexture.height);
            Screen.showCursor = false;
            Screen.lockCursor = true;
        }

        void Update()
        {
            HandleDebugInput();

            UpdateBlockSelectionType();

            Block hitBlock;
            Vector3 hitPosition;
            Vector3 hitNormal;
            Vector3 checkDirection = Camera.main.transform.forward;
            Vector3 origin = Camera.main.transform.position;
            origin.x += 1.0f;
            origin.y += 1.0f;
            origin.z += 1.0f;
            bool hit = ironVoxel.Service.CollisionService.RaytraceCollision(origin, checkDirection, reach, false, true, out hitBlock, out hitPosition, out hitNormal);

            UpdateBlockSelector(hit, hitPosition, hitNormal);
            HandlePlacingBlock(hit, hitPosition, hitNormal);
            HandleRemovingBlock(hit, hitPosition);
            HandleSpecialInput(hit, hitPosition, checkDirection);
        }

        private void UpdateBlockSelector(bool hit, Vector3 hitPosition, Vector3 hitNormal)
        {
            if (blockSelector != null && faceSelector != null) {
                if (hit && showUI) {
                    blockSelector.gameObject.SetActive(true);
                    faceSelector.gameObject.SetActive(true);
                    
                    Vector3 blockSelectorPosition;
                    blockSelectorPosition.x = Mathf.Floor(hitPosition.x - 1.0f) + 0.5f;
                    blockSelectorPosition.y = Mathf.Floor(hitPosition.y - 1.0f) + 0.5f;
                    blockSelectorPosition.z = Mathf.Floor(hitPosition.z - 1.0f) + 0.5f;
                    blockSelector.transform.position = blockSelectorPosition;
                    
                    Vector3 faceSelectorPosition = blockSelectorPosition + hitNormal * 0.51f;
                    faceSelector.transform.position = faceSelectorPosition;
                    if (-hitNormal != Vector3.zero) {
                        faceSelector.transform.rotation = Quaternion.LookRotation(-hitNormal);
                    }
                }
                else {
                    blockSelector.gameObject.SetActive(false);
                    faceSelector.gameObject.SetActive(false);
                }
            }
        }

        private void HandlePlacingBlock(bool hit, Vector3 hitPosition, Vector3 hitNormal)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0)) {
                BlockSpacePosition newBlockPosition = BlockSpacePosition.CreateFromVector3(hitPosition + hitNormal);
                if (hit && ironVoxel.Service.CollisionService.PotentialEntityCollision(newBlockPosition) == false) {
                    Vector3 setPosition = hitPosition + hitNormal;
                    ironVoxel.Service.ChunkRepository.SetBlockAtPosition(setPosition, buildType);
                }
            }
        }

        private void HandleRemovingBlock(bool hit, Vector3 hitPosition)
        {
            if (Input.GetKeyDown(KeyCode.Mouse1)) {
                Vector3 blockPosition;
                blockPosition.x = Mathf.Floor(hitPosition.x - 1.0f) + 0.5f;
                blockPosition.y = Mathf.Floor(hitPosition.y - 1.0f) + 0.5f;
                blockPosition.z = Mathf.Floor(hitPosition.z - 1.0f) + 0.5f;
                
                if (hit) {
                    Vector3 drawSize;
                    drawSize.x = 0.52f;
                    drawSize.y = 0.52f;
                    drawSize.z = 0.52f;
                    DebugUtils.DrawCube(blockPosition, drawSize, Color.blue, 0, true);
                    
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                        ironVoxel.Service.ChunkRepository.RemoveBlocksWithinRadius(hitPosition, 3.0f, BlockParticle.CreateBlockParticle);
                    }
                    else {
                        ironVoxel.Service.ChunkRepository.RemoveBlockAtPosition(hitPosition, BlockParticle.CreateBlockParticle);
                    }
                }
            }
        }

        private void HandleSpecialInput(bool hit, Vector3 hitPosition, Vector3 faceDirection)
        {
            if (Input.GetKeyDown(KeyCode.Mouse2)) {
                if (explosiveBallPrefab != null) {
                    GameObject explosiveBall = Instantiate(explosiveBallPrefab, transform.position + faceDirection + Vector3.up, Quaternion.identity) as GameObject;
                    BallPhysics ballPhysics = explosiveBall.GetComponent<BallPhysics>();
                    Vector3 force = faceDirection;
                    float tossSpeed = 0.8f;
                    force.x = force.x * tossSpeed;
                    force.y = force.y * tossSpeed;
                    force.z = force.z * tossSpeed;
                    ballPhysics.applyForce(force);
                }
            }
        }

        private void HandleDebugInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                Screen.lockCursor = !Screen.lockCursor;
                Screen.showCursor = !Screen.lockCursor;
            }
            
            if (Application.isWebPlayer) {
                Screen.lockCursor = !Screen.lockCursor;
                Screen.lockCursor = !Screen.lockCursor;
            }
            
            if (Input.GetKeyDown(KeyCode.F)) {
                RenderSettings.fog = !RenderSettings.fog;
            }
            
            if (Input.GetKeyDown(KeyCode.F11)) {
                if (reach == STANDARD_REACH) {
                    reach = DEVELOPER_REACH;
                }
                else {
                    reach = STANDARD_REACH;
                }
            }

            if (Input.GetKeyDown(KeyCode.Backspace)) {
                showUI = !showUI;
            }
        }

        private void UpdateBlockSelectionType()
        {
            if (Input.GetAxis("Mouse ScrollWheel") < 0 || Input.GetKeyDown(KeyCode.LeftArrow)) {
                IncrementBuildType();
            }
            
            if (Input.GetAxis("Mouse ScrollWheel") > 0 || Input.GetKeyDown(KeyCode.RightArrow)) {
                DecrementBuildType();
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                buildType = BlockType.Cobble;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2)) {
                buildType = BlockType.Stone;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3)) {
                buildType = BlockType.Wood;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4)) {
                buildType = BlockType.WoodPlanks;
            }
            if (Input.GetKeyDown(KeyCode.Alpha5)) {
                buildType = BlockType.Dirt;
            }
            if (Input.GetKeyDown(KeyCode.Alpha6)) {
                buildType = BlockType.Sand;
            }
            if (Input.GetKeyDown(KeyCode.Alpha7)) {
                buildType = BlockType.Glass;
            }
            if (Input.GetKeyDown(KeyCode.Alpha8)) {
                buildType = BlockType.Lamp;
            }
            if (Input.GetKeyDown(KeyCode.Alpha9)) {
                buildType = BlockType.Lava;
            }
        }

        private bool IsValidBlockTypeToBuild(BlockType blockType)
        {
            return blockType != BlockType.Air &&
                blockType != BlockType.Bedrock;
        }

        private void IncrementBuildType()
        {
            buildTypeIndex++;
            buildTypeIndex = buildTypeIndex % (int)BlockType.NumberOfBlockTypes;

            buildType = (BlockType)buildTypeIndex;
            if (!IsValidBlockTypeToBuild(buildType)) {
                IncrementBuildType();
            }
        }

        private void DecrementBuildType()
        {
            buildTypeIndex--;
            if (buildTypeIndex < 0) {
                buildTypeIndex += (int)BlockType.NumberOfBlockTypes;
            }
            
            buildType = (BlockType)buildTypeIndex;
            if (!IsValidBlockTypeToBuild(buildType)) {
                DecrementBuildType();
            }
        }

        private static Rect uiBuildTypeTextRect = new Rect();

        void OnGUI()
        {
            if (showUI) {
                // Reticle
                Vector2 center;
                center.x = Screen.width / 2.0f;
                center.y = Screen.height / 2.0f;
                reticlePosition.center = center;
                GUI.DrawTexture(reticlePosition, crosshairTexture);

                // Build type
                uiBuildTypeTextRect.x = 0;
                uiBuildTypeTextRect.y = 0;
                uiBuildTypeTextRect.width = 200;
                uiBuildTypeTextRect.height = 20;
                GUI.TextArea(uiBuildTypeTextRect, "Build Block: " + buildType.ToString());
            }
        }
    }
}