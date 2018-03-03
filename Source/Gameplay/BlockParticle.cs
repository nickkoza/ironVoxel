// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;
using ironVoxel.Gameplay;
using ironVoxel.Domain;
using ironVoxel.Service;
using ironVoxel;

public class BlockParticle : MonoBehaviour {
    private static int ringBufferSize = 250;
    private static BlockParticle[] ringBuffer = new BlockParticle[ringBufferSize];
    private static int ringBufferPosition = 0;
    private static bool ringBufferInitialized = false;
    private BallPhysics ballPhysics;
    private int destroyCountdown;
    private Vector3 startingScale;
    private int scaleDownTime;

    public Mesh CubeMesh { private get; set; }

    public void Initialize()
    {
        Vector3[] vertices = new Vector3[4 * 6];
        Vector3[] normals = new Vector3[4 * 6];
        int[] triangles = new int[6 * 6];
        Vector2[] uvs = new Vector2[4 * 6];
        Color[] colors = new Color[4 * 6];
        Mesh mesh = new Mesh();
        float s = 1.0f;

        Vector3 normal;
        int i = 0;

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>() as MeshFilter;
        meshFilter.mesh = mesh;
        CubeMesh = mesh;

        // Top
        normal = Vector3.up;
        vertices[i] = new Vector3(-s, s, -s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(-s, s, s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(s, s, -s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(s, s, s);
        normals[i] = normal;
        i++;

        // Bottom
        normal = Vector3.down;
        vertices[i] = new Vector3(s, -s, -s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(s, -s, s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(-s, -s, -s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(-s, -s, s);
        normals[i] = normal;
        i++;

        // North
        normal = Vector3.back;
        vertices[i] = new Vector3(-s, -s, -s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(-s, s, -s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(s, -s, -s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(s, s, -s);
        normals[i] = normal;
        i++;

        // South
        normal = Vector3.forward;
        vertices[i] = new Vector3(s, -s, s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(s, s, s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(-s, -s, s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(-s, s, s);
        normals[i] = normal;
        i++;

        // West
        normal = Vector3.left;
        vertices[i] = new Vector3(-s, -s, s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(-s, s, s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(-s, -s, -s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(-s, s, -s);
        normals[i] = normal;
        i++;

        // East
        normal = Vector3.right;
        vertices[i] = new Vector3(s, -s, -s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(s, s, -s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(s, -s, s);
        normals[i] = normal;
        i++;
        vertices[i] = new Vector3(s, s, s);
        normals[i] = normal;
        i++;

        // Finish
        BlockDefinition blockDefinition = BlockDefinition.DefinitionOfType(BlockType.Dirt);
        Block block = new Block();
        block.Set(blockDefinition);

        for (int j = 0; j < 6; j++) {
            Vector2 textureCoordinates = block.GetTextureCoordinates(CubeSide.Top, true, true, (byte)255);
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
            uvs[j * 4 + 0] = uv;

            uv.x = lowerUVs.x;
            uv.y = lowerUVs.y;
            uvs[j * 4 + 1] = uv;

            uv.x = upperUVs.x;
            uv.y = upperUVs.y;
            uvs[j * 4 + 2] = uv;

            uv.x = upperUVs.x;
            uv.y = lowerUVs.y;
            uvs[j * 4 + 3] = uv;

            triangles[j * 6 + 0] = j * 4 + 0;
            triangles[j * 6 + 1] = j * 4 + 1;
            triangles[j * 6 + 2] = j * 4 + 2;
            triangles[j * 6 + 3] = j * 4 + 1;
            triangles[j * 6 + 4] = j * 4 + 3;
            triangles[j * 6 + 5] = j * 4 + 2;

            colors[j * 4 + 0] = Color.white;
            colors[j * 4 + 1] = Color.white;
            colors[j * 4 + 2] = Color.white;
            colors[j * 4 + 3] = Color.white;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.RecalculateBounds();
        mesh.Optimize();
        mesh.UploadMeshData(false);

        gameObject.SetActive(false);

        startingScale = transform.localScale * 0.5f;
    }
    
    void Start()
    {
        destroyCountdown = 60 * 3;
        scaleDownTime = 60 * 1;
        
        ballPhysics = GetComponent<BallPhysics>();
        
        Vector3 force;
        force.x = Random.Range(-0.02f, 0.02f);
        force.y = Random.Range(0.05f, 0.1f);
        force.z = Random.Range(-0.02f, 0.02f);
        ballPhysics.applyForce(force);
        
        Quaternion rotation = transform.rotation;
        Vector3 eulerAngles = rotation.eulerAngles;
        eulerAngles.y = Random.Range(0.0f, 360.0f);
        rotation.eulerAngles = eulerAngles;
        transform.rotation = rotation;
    }
    
    void Update()
    {
        Quaternion rotation = transform.rotation;
        Vector3 eulerAngles = rotation.eulerAngles;
        eulerAngles.y += 2.0f;
        rotation.eulerAngles = eulerAngles;
        transform.rotation = rotation;
        
        if (destroyCountdown < scaleDownTime) {
            transform.localScale = startingScale * ((float)destroyCountdown / (float)scaleDownTime);
        }
        else {
            transform.localScale = startingScale;
        }
        
        destroyCountdown--;
        if (destroyCountdown <= 0) {
            gameObject.SetActive(false);
        }
    }
    
    public void AddExplosionForce(Vector3 explosionOrigin)
    {
        Vector3 direction = (explosionOrigin - transform.position).normalized;
        float speed = Random.Range(0.1f, 0.2f);
        Vector3 force;
        force.x = direction.x * speed;
        force.y = direction.y * speed;
        force.z = direction.z * speed;
        ballPhysics.applyForce(force);
    }

    public void UpdateTexture(BlockSpacePosition blockPosition, Block block)
    {
        Vector2 textureCoordinates = block.GetTextureCoordinates(CubeSide.Top, true, true, (byte)254);
        Vector2 overallTextureSize = block.GetOverallTextureSize();
        Vector2 individualTextureSize = block.GetIndividualTextureSize();
        
        Vector2 lowerUVs, upperUVs;
        lowerUVs.x = textureCoordinates.x / overallTextureSize.x;
        lowerUVs.y = 1.0f - textureCoordinates.y / overallTextureSize.y;
        upperUVs.x = (textureCoordinates.x + individualTextureSize.x) / overallTextureSize.x;
        upperUVs.y = 1.0f - (textureCoordinates.y + individualTextureSize.y) / overallTextureSize.y;

        Vector2[] uvs = CubeMesh.uv;
        for (int j = 0; j < 6; j++) {
            Vector2 uv;
            uv.x = lowerUVs.x;
            uv.y = upperUVs.y;
            uvs[j * 4 + 0] = uv;
            
            uv.x = lowerUVs.x;
            uv.y = lowerUVs.y;
            uvs[j * 4 + 1] = uv;
            
            uv.x = upperUVs.x;
            uv.y = upperUVs.y;
            uvs[j * 4 + 2] = uv;
            
            uv.x = upperUVs.x;
            uv.y = lowerUVs.y;
            uvs[j * 4 + 3] = uv;
        }
        CubeMesh.uv = uvs;


        Color lightColor = RenderService.SampleLight(blockPosition, CubeSide.Top);
        Color[] colors = CubeMesh.colors;
        for (int j = 0; j < 4 * 6; j++) {
            colors[j] = lightColor;
        }
        CubeMesh.colors = colors;
    }
    
    public static void CreateBlockParticle(BlockSpacePosition blockPosition, Block block)
    {
        if (block.IsNotActive()) {
            return;
        }
        
        Vector3 position;
        position.x = blockPosition.x - 0.5f;
        position.y = blockPosition.y;
        position.z = blockPosition.z - 0.5f;
        BlockParticle createdBlockParticle = CreateBlockParticle(position);
        createdBlockParticle.UpdateTexture(blockPosition, block);
    }
    
    private static void InitializeRingBuffer()
    {
        if (ringBufferInitialized) {
            return;
        }
        
        for (int i = 0; i < ringBufferSize; i++) {
            BlockParticle particle = (BlockParticle)Instantiate(World.Instance().blockParticle, Vector3.zero, Quaternion.identity);
            particle.Initialize();
            ringBuffer[i] = particle;
        }
        
        ringBufferPosition = 0;
        ringBufferInitialized = true;
    }
    
    private static BlockParticle CreateBlockParticle(Vector3 position)
    {
        if (!ringBufferInitialized) {
            InitializeRingBuffer();
        }
        BlockParticle particle = ringBuffer[ringBufferPosition];
        particle.gameObject.SetActive(true);
        particle.transform.position = position;
        particle.Start();
        
        ringBufferPosition = (ringBufferPosition + 1) % ringBufferSize;
        return particle;
    }
}