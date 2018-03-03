// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

namespace ironVoxel {
    public sealed class Configuration {
        // ---- OVERALL ----
        // This controls how high up from bedrock the player can build. You can adjust this based on the hardware you're targetting (strong hardware can handle
        // taller worlds.) You should keep it a multiple of Chunk.SIZE.
        public static readonly ushort HEIGHT = 128;

        // Only [CHUNK_VIEW_DISTANCE - CHUNK_GENERATE_GAP] number of chunks will ever be automatically loaded into the game. But a
        // full CHUNK_VIEW_DISTANCE worth of chunks will be left in the game once loaded. This allows players to walk around
        // a bit without triggering chunk loading/unloading.
        public static readonly int CHUNK_VIEW_DISTANCE = 7;
        public static readonly int CHUNK_GENERATE_GAP = 2;

        // ---- LIGHTING ----
        // These can be made non-readonly if you want dynamic sunlight. Just be sure to regenerate your chunk meshes after making a change, and not to change
        //it too often (regenerating chunk meshes is expensive.)
        public static readonly byte SUNLIGHT_HUE = 0;
        public static readonly byte SUNLIGHT_SATURATION = 0;
        public static readonly byte SUNLIGHT_VALUE = 255;
        public static readonly Vector3 SUN_ANGLE = new Vector3(-0.1f, 1.0f, -0.1f).normalized;

        // AMBIENT_LIGHT_* is how much ambient is automatically added on the surface, while AMBIENT_LIGHT_*_SUBTERRANEAN is how much ambient is automatically
        // added while deep underground.
        public static readonly byte AMBIENT_LIGHT_HUE = 0;
        public static readonly byte AMBIENT_LIGHT_SATURATION = 0;
        public static readonly byte AMBIENT_LIGHT_VALUE = 64;
        public static readonly byte AMBIENT_LIGHT_HUE_SUBTERRANEAN = 0;
        public static readonly byte AMBIENT_LIGHT_SATURATION_SUBTERRANEAN = 0;
        public static readonly byte AMBIENT_LIGHT_VALUE_SUBTERRANEAN = 24;

        public static readonly float AMBIENT_SUBTERRANEAN_START_HEIGHT = 64.0f;
        public static readonly float AMBIENT_SUBTERRANEAN_FULL_HEIGHT = 30.0f;

        // Use caution when increasing the max light radius - it has significant performance implications
        public static readonly ushort MAX_LIGHT_RADIUS = 10;

        // ---- MODELS ----
        public static readonly ushort MAX_MODEL_RADIUS = 10;

        // ---- PERFORMANCE TUNING ----
        // These "deadline" variables control various timing components of the asyncronous loading systems. They control what percentage of a frame is allowed
        // to have already elapsed before it will not consider starting new work, because it likely can't complete the work before the frame is scheduled to
        // finish.
        // - Increasing these toward 1.0 will cause work to happen faster, but it may cause framerate hitching.
        // - Decreasing these toward 0.0 will prevent framerate hitching, but will use the available frame time less efficently and not get work done as quickly.
        public static readonly double PERFORMANCE_START_WORK_DEADLINE = 0.8;
        public static readonly double PERFORMANCE_GENERATE_NEW_CHUNKS_DEADLINE = 0.8;
        public static readonly double PERFORMANCE_FLUSH_MODIFICATIONS_DEADLINE = 0.8;
        public static readonly double PERFORMANCE_FINISH_MESH_DEADLINE = 0.4;
        public static readonly double PERFORMANCE_GENERATE_MESH_DEADLINE = 0.5;
        public static readonly double PERFORMANCE_MARK_CHUNKS_FOR_MESH_UPDATE_DEADLINE = 0.5;
        public static readonly double PERFORMANCE_GENERATE_BLOCKS_DEADLINE = 0.5;

        // These "max" variables control the size of various pools and lists. You should only need to adjust these if you're largely changing the size of the
        // world that you're generating.
        public static readonly int PERFORMANCE_MAX_THREAD_QUEUE_SIZE = 2000;
        public static readonly int PERFORMANCE_MAX_PROCESSING_CHUNK_LIST_REPRIORITIZE_BATCH = 400;
        public static readonly int PERFORMANCE_MAX_THREAD_JOB_LIST_REPRIORITIZE_BATCH = 400;

    }
}
