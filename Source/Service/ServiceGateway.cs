// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

namespace ironVoxel.Service {
    public class ServiceGateway<T> where T : IService, new() {
        private static T instance;
        protected static bool initialized = false;
        private static object servicePadlock = new object();
        
        protected ServiceGateway ()
        {
        }

        public static void Initialize()
        {
            instance = new T();
            initialized = true;
        }

        protected static T Instance()
        {
            lock (servicePadlock) {
                if (initialized == false) {
                    throw new System.Exception("Service not initialized");
                }
            }
            return instance;
        }
    }
}