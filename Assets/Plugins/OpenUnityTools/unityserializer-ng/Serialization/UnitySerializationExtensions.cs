using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using System.Collections.Generic;
using Serialization;
using System.Linq;
using System.Reflection;

[Serializer(typeof(Vector2))]
public class SerializeVector2 : SerializerExtensionBase<Vector2> {
    public override IEnumerable<object> Save(Vector2 target) {
        return new object[] { target.x, target.y };
    }

    public override object Load(object[] data, object instance) {
#if US_LOGGING
		Radical.Log ("Vector3: {0},{1},{2}", data [0], data [1], data [2]);
#endif
        return new Vector2((float)data[0], (float)data[1]);
    }
}

[Serializer(typeof(Vector3))]
public class SerializeVector3 : SerializerExtensionBase<Vector3> {
    public override IEnumerable<object> Save(Vector3 target) {
        return new object[] { target.x, target.y, target.z };
    }

    public override object Load(object[] data, object instance) {
#if US_LOGGING
		Radical.Log ("Vector3: {0},{1},{2}", data [0], data [1], data [2]);
#endif
        return new UnitySerializer.DeferredSetter(d => {
            if (!float.IsNaN((float)data[0]) && !float.IsNaN((float)data[1]) && !float.IsNaN((float)data[2]))
                return new Vector3((float)data[0], (float)data[1], (float)data[2]);
            else
                return Vector3.zero;
        }
        );
    }
}

[Serializer(typeof(Vector4))]
public class SerializeVector4 : SerializerExtensionBase<Vector4> {
    public override IEnumerable<object> Save(Vector4 target) {
        return new object[] { target.x, target.y, target.z, target.w };
    }

    public override object Load(object[] data, object instance) {
#if US_LOGGING
		Radical.Log ("Vector3: {0},{1},{2}", data [0], data [1], data [2]);
#endif
        if (!float.IsNaN((float)data[0]) && !float.IsNaN((float)data[1]) && !float.IsNaN((float)data[2]) && !float.IsNaN((float)data[3]))
            return new Vector4((float)data[0], (float)data[1], (float)data[2], (float)data[3]);
        else
            return Vector4.zero;
    }
}

[Serializer(typeof(Quaternion))]
public class SerializeQuaternion : SerializerExtensionBase<Quaternion> {
    public override IEnumerable<object> Save(Quaternion target) {

        return new object[] { target.x, target.y, target.z, target.w };
    }

    public override object Load(object[] data, object instance) {
        return new UnitySerializer.DeferredSetter(d => new Quaternion((float)data[0], (float)data[1], (float)data[2], (float)data[3]));
    }
}

[Serializer(typeof(Color))]
public class SerializeColor : SerializerExtensionBase<Color> {
    public override IEnumerable<object> Save(Color target) {
        return new object[] { target.r, target.g, target.b, target.a };
    }

    public override object Load(object[] data, object instance) {
#if US_LOGGING
		Radical.Log ("Vector3: {0},{1},{2}", data [0], data [1], data [2]);
#endif
        return new Color((float)data[0], (float)data[1], (float)data[2], (float)data[3]);
    }
}



[Serializer(typeof(AnimationState))]
public class SerializeAnimationState : SerializerExtensionBase<AnimationState> {
    public override IEnumerable<object> Save(AnimationState target) {
        return new object[] { target.name };
    }

    public override object Load(object[] data, object instance) {
        var uo = UnitySerializer.DeserializingObject;
        return new UnitySerializer.DeferredSetter(d => {
            var p = uo.GetType().GetProperty("animation").GetGetMethod();
            if (p != null) {
                var animation = (Animation)p.Invoke(uo, null);
                if (animation != null) {
                    return animation[(string)data[0]];

                }
            }
            return null;
        });
    }
}

[Serializer(typeof(WaitForSeconds))]
public class SerializeWaitForSeconds : SerializerExtensionBase<WaitForSeconds> {

    public override IEnumerable<object> Save(WaitForSeconds target) {
        var tp = target.GetType();
        var f = tp.GetField("m_Seconds", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return new object[] { f.GetValue(target) };
    }

    public override object Load(object[] data, object instance) {
        return new WaitForSeconds((float)data[0]);
    }
}

[Serializer(typeof(Bounds))]
public class SerializeBounds : SerializerExtensionBase<Bounds> {
    public override IEnumerable<object> Save(Bounds target) {
        return new object[] { target.center.x, target.center.y, target.center.z, target.size.x, target.size.y, target.size.z };
    }

    public override object Load(object[] data, object instance) {
        return new Bounds(
                new Vector3((float)data[0], (float)data[1], (float)data[2]),
                new Vector3((float)data[3], (float)data[4], (float)data[5]));
    }
}

public abstract class ComponentSerializerExtensionBase<T> : IComponentSerializer where T : Component {
    public abstract IEnumerable<object> Save(T target);


    public abstract void LoadInto(object[] data, T instance);

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        return UnitySerializer.Serialize(Save((T)component).ToArray());

    }

    public void Deserialize(byte[] data, Component instance) {
        object[] dataArray;
        dataArray = UnitySerializer.Deserialize<object[]>(data);
        LoadInto(dataArray, (T)instance);
    }
    #endregion
}

public class SerializerExtensionBase<T> : ISerializeObjectEx {
    #region ISerializeObject implementation
    public object[] Serialize(object target) {
        return Save((T)target).ToArray();
    }

    public object Deserialize(object[] data, object instance) {
        return Load(data, instance);
    }
    #endregion

    public virtual IEnumerable<object> Save(T target) {
        return new object[0];
    }

    public virtual object Load(object[] data, object instance) {
        return null;
    }

    #region ISerializeObjectEx implementation
    public bool CanSerialize(Type targetType, object instance) {
        if (instance == null)
            return true;
        return CanBeSerialized(targetType, instance);
    }
    #endregion

    public virtual bool CanBeSerialized(Type targetType, object instance) {
        return true;
    }


}

[ComponentSerializerFor(typeof(BoxCollider))]
public class SerializeBoxCollider : ComponentSerializerExtensionBase<BoxCollider> {
    public override IEnumerable<object> Save(BoxCollider target) {
        return new object[] { target.isTrigger, target.size.x, target.size.y, target.size.z, target.center.x, target.center.y, target.center.z, target.enabled, target.sharedMaterial };
    }
    public override void LoadInto(object[] data, BoxCollider instance) {
        instance.isTrigger = (bool)data[0];
        instance.size = new Vector3((float)data[1], (float)data[2], (float)data[3]);
        instance.center = new Vector3((float)data[4], (float)data[5], (float)data[6]);
        instance.enabled = (bool)data[7];
        instance.sharedMaterial = (PhysicMaterial)data[8];
    }
}

[ComponentSerializerFor(typeof(Terrain))]
public class SerializeTerrain : ComponentSerializerExtensionBase<Terrain> {
    public override IEnumerable<object> Save(Terrain target) {
        return new object[] { target.enabled };
    }

    public override void LoadInto(object[] data, Terrain instance) {
        instance.enabled = (bool)data[0];
    }
}

[ComponentSerializerFor(typeof(TerrainCollider))]
public class SerializeCollider : ComponentSerializerExtensionBase<TerrainCollider> {
    public override IEnumerable<object> Save(TerrainCollider target) {
        return new object[] { target.sharedMaterial, target.terrainData, target.enabled };
    }

    public override void LoadInto(object[] data, TerrainCollider instance) {
        instance.sharedMaterial = (PhysicMaterial)data[0];
        instance.terrainData = (TerrainData)data[1];
        instance.enabled = (bool)data[2];
    }
}

[ComponentSerializerFor(typeof(MeshCollider))]
public class SerializeMeshCollider : ComponentSerializerExtensionBase<MeshCollider> {
    public override IEnumerable<object> Save(MeshCollider target) {
        return new object[] { target.convex, target.isTrigger, target.sharedMaterial, target.sharedMesh, target.enabled };
    }

    public override void LoadInto(object[] data, MeshCollider instance) {
        instance.convex = (bool)data[0];
        instance.isTrigger = (bool)data[1];
        instance.sharedMaterial = (PhysicMaterial)data[2];
        instance.sharedMesh = (Mesh)data[3];
        instance.enabled = (bool)data[4];
    }
}

[ComponentSerializerFor(typeof(WheelCollider))]
public class SerializeWheelCollider : IComponentSerializer {
    public class StoredInformation {
        public bool Enabled;
        public float brakeTorque;
        public Vector3 center;
        public float forceAppPointDistance;
        public WheelFrictionCurve forwardFriction;
        public float mass;
        public float motorTorque;
        public float radius;
        public WheelFrictionCurve sidewaysFriction;
        public float steerAngle;
        public float suspensionDistance;
        public JointSpring suspensionSpring;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        using (new UnitySerializer.SerializationSplitScope()) {
            var collider = (WheelCollider)component;
            var si = new StoredInformation();
            si.Enabled = collider.enabled;
            si.brakeTorque = collider.brakeTorque;
            si.center = collider.center;
            si.forceAppPointDistance = collider.forceAppPointDistance;
            si.forwardFriction = collider.forwardFriction;
            si.mass = collider.mass;
            si.motorTorque = collider.motorTorque;
            si.radius = collider.radius;
            si.sidewaysFriction = collider.sidewaysFriction;
            si.steerAngle = collider.steerAngle;
            si.suspensionDistance = collider.suspensionDistance;
            si.suspensionSpring = collider.suspensionSpring;
            var data = UnitySerializer.Serialize(si);
            return data;
        }
    }

    public void Deserialize(byte[] data, Component instance) {
        var collider = (WheelCollider)instance;
        collider.enabled = false;
        UnitySerializer.AddFinalAction(() => {
            using (new UnitySerializer.SerializationSplitScope()) {
                var si = UnitySerializer.Deserialize<StoredInformation>(data);
                if (si == null) {
                    Debug.LogError("An error occured when getting the stored information for a WheelCollider");
                    return;
                }
                collider.enabled = si.Enabled;
                collider.brakeTorque = si.brakeTorque;
                collider.center = si.center;
                collider.forceAppPointDistance = si.forceAppPointDistance;
                collider.forwardFriction = si.forwardFriction;
                collider.mass = si.mass;
                collider.motorTorque = si.motorTorque;
                collider.radius = si.radius;
                collider.sidewaysFriction = si.sidewaysFriction;
                collider.steerAngle = si.steerAngle;
                collider.suspensionDistance = si.suspensionDistance;
                collider.suspensionSpring = si.suspensionSpring;
            }
        }
        );
    }
    #endregion
}

[ComponentSerializerFor(typeof(CapsuleCollider))]
public class SerializeCapsuleCollider : ComponentSerializerExtensionBase<CapsuleCollider> {
    public override IEnumerable<object> Save(CapsuleCollider target) {
        return new object[] { target.isTrigger, target.radius, target.center.x, target.center.y, target.center.z, target.height, target.enabled, target.sharedMaterial, target.direction };
    }
    public override void LoadInto(object[] data, CapsuleCollider instance) {
        instance.isTrigger = (bool)data[0];
        instance.radius = (float)data[1];
        instance.center = new Vector3((float)data[2], (float)data[3], (float)data[4]);
        instance.height = (float)data[5];
        instance.enabled = (bool)data[6];
        instance.sharedMaterial = (PhysicMaterial)data[7];
        instance.direction = (int)data[8];
    }
}

[ComponentSerializerFor(typeof(SphereCollider))]
public class SerializeSphereCollider : ComponentSerializerExtensionBase<SphereCollider> {
    public override IEnumerable<object> Save(SphereCollider target) {
        return new object[] { target.isTrigger, target.radius, target.center.x, target.center.y, target.center.z, target.enabled, target.sharedMaterial };
    }
    public override void LoadInto(object[] data, SphereCollider instance) {
        instance.isTrigger = (bool)data[0];
        instance.radius = (float)data[1];
        instance.center = new Vector3((float)data[2], (float)data[3], (float)data[4]);
        instance.enabled = (bool)data[5];
        instance.sharedMaterial = (PhysicMaterial)data[6];
    }
}


[Serializer(typeof(Texture2D))]
public class SerializeTexture2D : SerializerExtensionBase<Texture2D> {

    public override IEnumerable<object> Save(Texture2D target) {
        if (target.GetInstanceID() >= 0) {
            return new object[] { true, SaveGameManager.Instance.GetAssetId(target) };
        }
        else {
            return new object[] { 
				false,
				target.anisoLevel,
				target.filterMode,
				target.format,
				target.mipMapBias,
				target.mipmapCount,
				target.name,
				target.texelSize,
				target.wrapMode,
				target.EncodeToPNG()
			};
        }
    }

    public override object Load(object[] data, object instance) {
        if ((bool)data[0]) {
            return SaveGameManager.Instance.GetAsset((SaveGameManager.AssetReference)data[1]);
        }
        Texture2D t;
        if (data.Length == 10) {
            t = new Texture2D(1, 1, (TextureFormat)data[3], (int)data[5] > 0 ? true : false);
            t.LoadImage((byte[])data[9]);
            t.anisoLevel = (int)data[1];
            t.filterMode = (FilterMode)data[2];
            t.mipMapBias = (float)data[4];
            t.name = (string)data[6];
            t.wrapMode = (TextureWrapMode)data[8];
            t.Apply();

            return t;
        }

        t = new Texture2D((int)data[9], (int)data[4], (TextureFormat)data[3], (int)data[6] > 0 ? true : false);
        t.anisoLevel = (int)data[1];
        t.filterMode = (FilterMode)data[2];
        t.mipMapBias = (float)data[5];
        t.name = (string)data[7];
        t.wrapMode = (TextureWrapMode)data[10];
        t.SetPixels32((Color32[])data[11]);
        t.Apply();

        return t;
    }

    public override bool CanBeSerialized(Type targetType, object instance) {
        var obj = instance as Texture2D;
        if (!obj) {
            return false;
        }
        if (obj.GetInstanceID() < 0 || SaveGameManager.Instance.GetAssetId(instance as UnityEngine.Object).index != -1) {
            return true;
        }
        return false;
    }
}

[Serializer(typeof(Material))]
public class SerializeMaterial : SerializerExtensionBase<Material> {
    public override IEnumerable<object> Save(Material target) {
        var store = GetStore();
        if (target.GetInstanceID() >= 0) {
            return new object[] { true, SaveGameManager.Instance.GetAssetId(target) };
        }
        else {
            return new object[] { false, target.shader.name, target.name, target.renderQueue, store != null ? store.GetValues(target) : null };
        }

    }

    StoreMaterials GetStore() {
        if (SerializeRenderer.Store != null)
            return SerializeRenderer.Store;
        if (UnitySerializer.currentlySerializingObject is Component) {
            var comp = UnitySerializer.currentlySerializingObject as Component;
            return comp.GetComponent<StoreMaterials>();
        }
        return null;
    }

    public override object Load(object[] data, object instance) {

        if ((bool)data[0]) {
            return SaveGameManager.Instance.GetAsset((SaveGameManager.AssetReference)data[1]);
        }
        Material m;
        var shdr = Shader.Find((string)data[1]);
        m = new Material(shdr);
        m.name = (string)data[2];
        var store = GetStore();
        m.renderQueue = (int)data[3];
        if (data[4] != null && store != null)
            store.SetValues(m, (List<StoreMaterials.StoredValue>)data[4]);
        return m;

    }

    public override bool CanBeSerialized(Type targetType, object instance) {

        var obj = instance as Material;
        if (!obj)
            return false;
        if ((Shader.Find(obj.shader.name) != null && obj.GetInstanceID() < 0 && SerializeRenderer.Store != null) || SaveGameManager.Instance.GetAssetId(instance as UnityEngine.Object).index != -1) {
            return true;
        }
        return false;
    }
}

[SubTypeSerializer(typeof(Texture))]
[Serializer(typeof(Font))]
[Serializer(typeof(AudioClip))]
[Serializer(typeof(TextAsset))]
[SubTypeSerializer(typeof(Mesh))]
[Serializer(typeof(AnimationClip))]
[Serializer(typeof(Sprite))]
[Serializer(typeof(Button.ButtonClickedEvent))]
[Serializer(typeof(Slider.SliderEvent))]
[Serializer(typeof(Scrollbar.ScrollEvent))]
[Serializer(typeof(Toggle.ToggleEvent))]
[Serializer(typeof(InputField.OnChangeEvent))]
[Serializer(typeof(InputField.SubmitEvent))]
public class SerializeAssetReference : SerializerExtensionBase<object> {
    public static SerializeAssetReference instance = new SerializeAssetReference();

    public override IEnumerable<object> Save(object target) {
        return new object[] { SaveGameManager.Instance.GetAssetId(target as UnityEngine.Object) };
    }

    public override bool CanBeSerialized(Type targetType, object instance) {

        return instance == null || typeof(UnityEngine.Object).IsAssignableFrom(targetType);
    }

    public override object Load(object[] data, object instance) {
        return SaveGameManager.Instance.GetAsset((SaveGameManager.AssetReference)data[0]);
    }
}

[SubTypeSerializer(typeof(ScriptableObject))]
public class SerializeScriptableObjectReference : SerializerExtensionBase<object> {
    public override IEnumerable<object> Save(object target) {
        var id = SaveGameManager.Instance.GetAssetId(target as UnityEngine.Object);
        if (id.index == -1) {
            byte[] data = null;
            data = UnitySerializer.SerializeForDeserializeInto(target);
            var result = new object[] { true, target.GetType().FullName, data };
            return result;
        }
        else {
            return new object[] { false, id };
        }

    }

    public override bool CanBeSerialized(Type targetType, object instance) {

        return instance != null;
    }

    public override object Load(object[] data, object instance) {
        if ((bool)data[0]) {
            var newInstance = ScriptableObject.CreateInstance(UnitySerializer.GetTypeEx(data[1]));
            UnitySerializer.DeserializeInto((byte[])data[2], newInstance);
            return newInstance;
        }
        else {
            return SaveGameManager.Instance.GetAsset((SaveGameManager.AssetReference)data[1]);
        }

    }
}



/// <summary>
/// Store a reference to a game object, first checking whether it is really another game
/// object and not a prefab
/// </summary>
[Serializer(typeof(GameObject))]
public class SerializeGameObjectReference : SerializerExtensionBase<GameObject> {
    static SerializeGameObjectReference() {
        UnitySerializer.CanSerialize += (tp) => {
            return !typeof(MeshFilter).IsAssignableFrom(tp);
        };
    }

    public override IEnumerable<object> Save(GameObject target) {
        //Is this a reference to a prefab
        var assetId = SaveGameManager.Instance.GetAssetId(target);
        if (assetId.index != -1) {
            return new object[] { 0, true, null, assetId };
        }
        return new object[] { target.GetId(), UniqueIdentifier.GetByName(target.gameObject.GetId()) != null /* Identify a prefab */ };
    }

    public override object Load(object[] data, object instance) {
        if (instance != null) {
            return instance;
        }
#if US_LOGGING
		if (!((bool)data [1])) {
			Radical.Log ("[[Disabled, will not be set]]");
		}
		Radical.Log ("GameObject: {0}", data [0]);
#endif
        if (data.Length > 3) {
            var asset = SaveGameManager.Instance.GetAsset((SaveGameManager.AssetReference)data[3]) as GameObject;
            return asset;
        }
        return instance ?? new UnitySerializer.DeferredSetter((d) => {
            return UniqueIdentifier.GetByName((string)data[0]);
        }) {
            enabled = (bool)data[1]
        };
    }

}

[ComponentSerializerFor(typeof(NavMeshAgent))]
public class SerializeNavMeshAgent : IComponentSerializer {
    public class StoredInfo {
        public bool hasPath, offMesh, autoBraking, autoTraverseOffMeshLink, autoRepath;
        public float x, y, z, speed, angularSpeed, height, offset, acceleration, radius, stoppingDistance;
        public int passable = -1, avoidancePriority;
        public ObstacleAvoidanceType obstacleAvoidanceType;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        var agent = (NavMeshAgent)component;
        return UnitySerializer.Serialize(new StoredInfo {
            x = agent.destination.x,
            y = agent.destination.y,
            z = agent.destination.z,
            speed = agent.speed,
            acceleration = agent.acceleration,
            angularSpeed = agent.angularSpeed,
            height = agent.height,
            offset = agent.baseOffset,
            hasPath = agent.hasPath,
            offMesh = agent.isOnOffMeshLink,
            passable = agent.areaMask,
            radius = agent.radius,
            stoppingDistance = agent.stoppingDistance,
            autoBraking = agent.autoBraking,
            obstacleAvoidanceType = agent.obstacleAvoidanceType,
            avoidancePriority = agent.avoidancePriority,
            autoTraverseOffMeshLink = agent.autoTraverseOffMeshLink,
            autoRepath = agent.autoRepath
        });
    }

    public void Deserialize(byte[] data, Component instance) {
        var path = new NavMeshPath();
        var agent = (NavMeshAgent)instance;
        agent.enabled = false;

        Loom.QueueOnMainThread(() => {
            var si = UnitySerializer.Deserialize<StoredInfo>(data);
            agent.speed = si.speed;
            agent.acceleration = si.acceleration;
            agent.angularSpeed = si.angularSpeed;
            agent.height = si.height;
            agent.baseOffset = si.offset;
            agent.areaMask = si.passable;
            agent.radius = si.radius;
            agent.stoppingDistance = si.stoppingDistance;
            agent.autoBraking = si.autoBraking;
            agent.obstacleAvoidanceType = si.obstacleAvoidanceType;
            agent.avoidancePriority = si.avoidancePriority;
            agent.autoTraverseOffMeshLink = si.autoTraverseOffMeshLink;
            agent.autoRepath = si.autoRepath;

            if (si.hasPath && !agent.isOnOffMeshLink) {
                agent.enabled = true;
                //if(agent.CalculatePath( new Vector3 (si.x, si.y, si.z), path))
                //{
                //		agent.SetPath(path);
                //	}


                if (NavMesh.CalculatePath(agent.transform.position, new Vector3(si.x, si.y, si.z), si.passable, path))
                    agent.SetPath(path);
            }
        }, 0.1f);


    }
    #endregion

}

[ComponentSerializerFor(typeof(Camera))]
public class SerializeCamera : IComponentSerializer {
    public class CameraData {
        public RenderingPath renderingPath;
        public float fieldOfView;
        public float nearClipPlane;
        public float farClipPlane;
        public float depth;
        public Rect rect;
        public bool useOcclusionCulling;
        public bool hdr;
        public RenderTexture targetTexture;
        public bool orthographic;
        public float orthographicSize;
        public Color backgroundColor;
    }
    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        var camera = (Camera)component;
        var cd = new CameraData {
            renderingPath = camera.actualRenderingPath,
            fieldOfView = camera.fieldOfView,
            depth = camera.depth,
            nearClipPlane = camera.nearClipPlane,
            farClipPlane = camera.farClipPlane,
            rect = camera.rect,
            useOcclusionCulling = camera.useOcclusionCulling,
            hdr = camera.hdr,
            targetTexture = camera.targetTexture,
            orthographic = camera.orthographic,
            orthographicSize = camera.orthographicSize,
            backgroundColor = camera.backgroundColor
        };
        return UnitySerializer.Serialize(cd);
    }

    public void Deserialize(byte[] data, Component instance) {
        var cd = UnitySerializer.Deserialize<CameraData>(data);
        var camera = (Camera)instance;
        camera.renderingPath = cd.renderingPath;
        camera.fieldOfView = cd.fieldOfView;
        camera.nearClipPlane = cd.nearClipPlane;
        camera.farClipPlane = cd.farClipPlane;
        camera.depth = cd.depth;
        camera.rect = cd.rect;
        camera.useOcclusionCulling = cd.useOcclusionCulling;
        camera.hdr = cd.hdr;
        camera.targetTexture = cd.targetTexture;
        camera.orthographic = cd.orthographic;
        camera.orthographicSize = cd.orthographicSize;
        camera.backgroundColor = cd.backgroundColor;
    }
    #endregion
}

[ComponentSerializerFor(typeof(Rigidbody))]
public class SerializeRigidBody : IComponentSerializer {
    public class RigidBodyInfo {
        public bool isKinematic;
        public bool useGravity, freezeRotation, detectCollisions, useConeFriction;
        public Vector3 velocity, position, angularVelocity, centerOfMass, inertiaTensor;
        public Quaternion rotation, inertiaTensorRotation;
        public float drag, angularDrag, mass, sleepThreshold, maxAngularVelocity;
        public RigidbodyConstraints constraints;
        public CollisionDetectionMode collisionDetectionMode;
        public RigidbodyInterpolation interpolation;
        public int solverIterationCount;
        public RigidBodyInfo() {
        }
        public RigidBodyInfo(Rigidbody source) {
            isKinematic = source.isKinematic;
            useGravity = source.useGravity;
            freezeRotation = source.freezeRotation;
            detectCollisions = source.detectCollisions;
            useConeFriction = source.useConeFriction;
            velocity = source.velocity;
            position = source.position;
            rotation = source.rotation;
            angularVelocity = source.angularVelocity;
            centerOfMass = source.centerOfMass;
            inertiaTensor = source.inertiaTensor;
            inertiaTensorRotation = source.inertiaTensorRotation;
            drag = source.drag;
            angularDrag = source.angularDrag;
            mass = source.mass;
            sleepThreshold = source.sleepThreshold;
            maxAngularVelocity = source.maxAngularVelocity;
            constraints = source.constraints;
            collisionDetectionMode = source.collisionDetectionMode;
            interpolation = source.interpolation;
            solverIterationCount = source.solverIterationCount;
        }

        public void Configure(Rigidbody body) {
            body.isKinematic = true;
            body.freezeRotation = freezeRotation;
            body.useGravity = useGravity;
            body.detectCollisions = detectCollisions;
            body.useConeFriction = useConeFriction;
            if (centerOfMass != Vector3.zero)
                body.centerOfMass = centerOfMass;
            body.drag = drag;
            body.angularDrag = angularDrag;
            body.mass = mass;
            if (!rotation.Equals(zero))
                body.rotation = rotation;
            body.sleepThreshold = sleepThreshold;
            body.maxAngularVelocity = maxAngularVelocity;
            body.constraints = constraints;
            body.collisionDetectionMode = collisionDetectionMode;
            body.interpolation = interpolation;
            body.solverIterationCount = solverIterationCount;
            body.isKinematic = isKinematic;
            if (!isKinematic) {
                body.velocity = velocity;
                body.useGravity = useGravity;
                body.angularVelocity = angularVelocity;
                if (inertiaTensor != Vector3.zero)
                    body.inertiaTensor = inertiaTensor;
                if (!inertiaTensorRotation.Equals(zero))
                    body.inertiaTensorRotation = inertiaTensorRotation;
            }

        }
    }

    static Quaternion zero = new Quaternion(0, 0, 0, 0);

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        return UnitySerializer.Serialize(new RigidBodyInfo((Rigidbody)component));

    }

    public void Deserialize(byte[] data, Component instance) {
        var info = UnitySerializer.Deserialize<RigidBodyInfo>(data);
        info.Configure((Rigidbody)instance);
        UnitySerializer.AddFinalAction(() => {
            info.Configure((Rigidbody)instance);
        });
    }
    #endregion

}

[SerializerPlugIn]
public class RagePixelSupport {
    static RagePixelSupport() {
        new SerializePrivateFieldOfType("RagePixelSprite", "animationPingPongDirection");
        new SerializePrivateFieldOfType("RagePixelSprite", "myTime");
    }
}

//[ComponentSerializerFor(typeof(Renderer))]
[ComponentSerializerFor(typeof(MeshRenderer))]
public class SerializeRenderer : IComponentSerializer {
    public static StoreMaterials Store;

    public class StoredInformation {
        public bool Enabled;
        public List<Material> materials = new List<Material>();
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
        public bool receiveShadows;
        public bool useLightProbes;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        using (new UnitySerializer.SerializationSplitScope()) {
            var renderer = (Renderer)component;
            var si = new StoredInformation();
            si.Enabled = renderer.enabled;
            if ((Store = renderer.GetComponent<StoreMaterials>()) != null) {

                si.materials = renderer.materials.ToList();
            }
            si.shadowCastingMode = renderer.shadowCastingMode;
            si.receiveShadows = renderer.receiveShadows;
            si.useLightProbes = renderer.useLightProbes;
            var data = UnitySerializer.Serialize(si);
            Store = null;
            return data;

        }
    }

    public void Deserialize(byte[] data, Component instance) {
        var renderer = (Renderer)instance;
        renderer.enabled = false;
        UnitySerializer.AddFinalAction(() => {
            Store = renderer.GetComponent<StoreMaterials>();

            using (new UnitySerializer.SerializationSplitScope()) {
                var si = UnitySerializer.Deserialize<StoredInformation>(data);
                if (si == null) {
                    Debug.LogError("An error occured when getting the stored information for a renderer");
                    return;
                }
                renderer.enabled = si.Enabled;
                if (si.materials.Count > 0) {
                    if (Store != null) {
                        renderer.materials = si.materials.ToArray();
                    }
                }
                renderer.shadowCastingMode = si.shadowCastingMode;
                renderer.receiveShadows = si.receiveShadows;
                renderer.useLightProbes = si.useLightProbes;
            }
            Store = null;
        }
        );
    }
    #endregion
}

[ComponentSerializerFor(typeof(LineRenderer))]
public class SerializeLineRenderer : IComponentSerializer {
    public static StoreMaterials Store;

    public class StoredInformation : SerializeRenderer.StoredInformation {
        public bool useWorldSpace;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        using (new UnitySerializer.SerializationSplitScope()) {
            var renderer = (LineRenderer)component;
            var si = new StoredInformation();
            si.Enabled = renderer.enabled;
            if ((Store = renderer.GetComponent<StoreMaterials>()) != null) {

                si.materials = renderer.materials.ToList();
            }
            si.shadowCastingMode = renderer.shadowCastingMode;
            si.receiveShadows = renderer.receiveShadows;
            si.useLightProbes = renderer.useLightProbes;
            si.useWorldSpace = renderer.useWorldSpace;
            var data = UnitySerializer.Serialize(si);
            Store = null;
            return data;

        }
    }

    public void Deserialize(byte[] data, Component instance) {
        var renderer = (LineRenderer)instance;
        renderer.enabled = false;
        UnitySerializer.AddFinalAction(() => {
            Store = renderer.GetComponent<StoreMaterials>();

            using (new UnitySerializer.SerializationSplitScope()) {
                var si = UnitySerializer.Deserialize<StoredInformation>(data);
                if (si == null) {
                    Debug.LogError("An error occured when getting the stored information for a LineRenderer");
                    return;
                }
                renderer.enabled = si.Enabled;
                if (si.materials.Count > 0) {
                    if (Store != null) {
                        renderer.materials = si.materials.ToArray();
                    }
                }
                renderer.shadowCastingMode = si.shadowCastingMode;
                renderer.receiveShadows = si.receiveShadows;
                renderer.useLightProbes = si.useLightProbes;
                renderer.useWorldSpace = si.useWorldSpace;
            }
            Store = null;
        }
        );
    }
    #endregion
}

[ComponentSerializerFor(typeof(TrailRenderer))]
public class SerializeTrailRenderer : IComponentSerializer {
    public static StoreMaterials Store;

    public class StoredInformation : SerializeRenderer.StoredInformation {
        public bool autodestruct;
        public float startWidth;
        public float endWidth;
        public float time;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        using (new UnitySerializer.SerializationSplitScope()) {
            var renderer = (TrailRenderer)component;
            var si = new StoredInformation();
            si.Enabled = renderer.enabled;
            if ((Store = renderer.GetComponent<StoreMaterials>()) != null) {

                si.materials = renderer.materials.ToList();
            }
            si.shadowCastingMode = renderer.shadowCastingMode;
            si.receiveShadows = renderer.receiveShadows;
            si.useLightProbes = renderer.useLightProbes;
            si.autodestruct = renderer.autodestruct;
            si.startWidth = renderer.startWidth;
            si.endWidth = renderer.endWidth;
            si.time = renderer.time;
            var data = UnitySerializer.Serialize(si);
            Store = null;
            return data;

        }
    }

    public void Deserialize(byte[] data, Component instance) {
        var renderer = (TrailRenderer)instance;
        renderer.enabled = false;
        UnitySerializer.AddFinalAction(() => {
            Store = renderer.GetComponent<StoreMaterials>();

            using (new UnitySerializer.SerializationSplitScope()) {
                var si = UnitySerializer.Deserialize<StoredInformation>(data);
                if (si == null) {
                    Debug.LogError("An error occured when getting the stored information for a TrailRenderer");
                    return;
                }
                renderer.enabled = si.Enabled;
                if (si.materials.Count > 0) {
                    if (Store != null) {
                        renderer.materials = si.materials.ToArray();
                    }
                }
                renderer.shadowCastingMode = si.shadowCastingMode;
                renderer.receiveShadows = si.receiveShadows;
                renderer.useLightProbes = si.useLightProbes;
                renderer.autodestruct = si.autodestruct;
                renderer.startWidth = si.startWidth;
                renderer.endWidth = si.endWidth;
                renderer.time = si.time;
            }
            Store = null;
        }
        );
    }
    #endregion
}

[ComponentSerializerFor(typeof(SkinnedMeshRenderer))]
public class SerializeSkinnedMeshRenderer : IComponentSerializer {
    public static StoreMaterials Store;

    public class StoredInformation : SerializeRenderer.StoredInformation {
        public Bounds localBounds;
        public SkinQuality quality;
        public bool updateWhenOffscreen;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        using (new UnitySerializer.SerializationSplitScope()) {
            var renderer = (SkinnedMeshRenderer)component;
            var si = new StoredInformation();
            si.Enabled = renderer.enabled;
            if ((Store = renderer.GetComponent<StoreMaterials>()) != null) {

                si.materials = renderer.materials.ToList();
            }
            si.shadowCastingMode = renderer.shadowCastingMode;
            si.receiveShadows = renderer.receiveShadows;
            si.useLightProbes = renderer.useLightProbes;
            si.localBounds = renderer.localBounds;
            si.quality = renderer.quality;
            si.updateWhenOffscreen = renderer.updateWhenOffscreen;
            var data = UnitySerializer.Serialize(si);
            Store = null;
            return data;

        }
    }

    public void Deserialize(byte[] data, Component instance) {
        var renderer = (SkinnedMeshRenderer)instance;
        renderer.enabled = false;
        UnitySerializer.AddFinalAction(() => {
            Store = renderer.GetComponent<StoreMaterials>();

            using (new UnitySerializer.SerializationSplitScope()) {
                var si = UnitySerializer.Deserialize<StoredInformation>(data);
                if (si == null) {
                    Debug.LogError("An error occured when getting the stored information for a SkinnedMeshRenderer");
                    return;
                }
                renderer.enabled = si.Enabled;
                if (si.materials.Count > 0) {
                    if (Store != null) {
                        renderer.materials = si.materials.ToArray();
                    }
                }
                renderer.shadowCastingMode = si.shadowCastingMode;
                renderer.receiveShadows = si.receiveShadows;
                renderer.useLightProbes = si.useLightProbes;
                renderer.localBounds = si.localBounds;
                renderer.quality = si.quality;
                renderer.updateWhenOffscreen = si.updateWhenOffscreen;
            }
            Store = null;
        }
        );
    }
    #endregion
}

[ComponentSerializerFor(typeof(AudioChorusFilter))]
public class SerializeAudioChorusFilter : IComponentSerializer {
    public class StoredInformation {
        public bool enabled;
        public float delay;
        public float depth;
        public float dryMix;
        public float rate;
        public float wetMix1;
        public float wetMix2;
        public float wetMix3;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        using (new UnitySerializer.SerializationSplitScope()) {
            var filter = (AudioChorusFilter)component;
            var si = new StoredInformation();
            si.enabled = filter.enabled;
            si.delay = filter.delay;
            si.depth = filter.depth;
            si.dryMix = filter.dryMix;
            si.rate = filter.rate;
            si.wetMix1 = filter.wetMix1;
            si.wetMix2 = filter.wetMix2;
            si.wetMix3 = filter.wetMix3;
            var data = UnitySerializer.Serialize(si);
            return data;
        }
    }

    public void Deserialize(byte[] data, Component instance) {
        var filter = (AudioChorusFilter)instance;
        filter.enabled = false;
        UnitySerializer.AddFinalAction(() => {
            using (new UnitySerializer.SerializationSplitScope()) {
                var si = UnitySerializer.Deserialize<StoredInformation>(data);
                if (si == null) {
                    Debug.LogError("An error occured when getting the stored information for a AudioChorusFilter");
                    return;
                }
                filter.delay = si.delay;
                filter.depth = si.depth;
                filter.dryMix = si.dryMix;
                filter.rate = si.rate;
                filter.wetMix1 = si.wetMix1;
                filter.wetMix2 = si.wetMix2;
                filter.wetMix3 = si.wetMix3;
                filter.enabled = si.enabled;
            }
        }
        );
    }
    #endregion
}

[ComponentSerializerFor(typeof(AudioDistortionFilter))]
public class SerializeAudioDistortionFilter : IComponentSerializer {
    public class StoredInformation {
        public bool enabled;
        public float distortionLevel;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        using (new UnitySerializer.SerializationSplitScope()) {
            var filter = (AudioDistortionFilter)component;
            var si = new StoredInformation();
            si.enabled = filter.enabled;
            si.distortionLevel = filter.distortionLevel;
            var data = UnitySerializer.Serialize(si);
            return data;
        }
    }

    public void Deserialize(byte[] data, Component instance) {
        var filter = (AudioDistortionFilter)instance;
        filter.enabled = false;
        UnitySerializer.AddFinalAction(() => {
            using (new UnitySerializer.SerializationSplitScope()) {
                var si = UnitySerializer.Deserialize<StoredInformation>(data);
                if (si == null) {
                    Debug.LogError("An error occured when getting the stored information for a AudioDistortionFilter");
                    return;
                }
                filter.distortionLevel = si.distortionLevel;
                filter.enabled = si.enabled;
            }
        }
        );
    }
    #endregion
}

[ComponentSerializerFor(typeof(AudioEchoFilter))]
public class SerializeAudioEchoFilter : IComponentSerializer {
    public class StoredInformation {
        public bool enabled;
        public float decayRatio;
        public float delay;
        public float dryMix;
        public float wetMix;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        using (new UnitySerializer.SerializationSplitScope()) {
            var filter = (AudioEchoFilter)component;
            var si = new StoredInformation();
            si.enabled = filter.enabled;
            si.decayRatio = filter.decayRatio;
            si.delay = filter.delay;
            si.wetMix = filter.wetMix;
            var data = UnitySerializer.Serialize(si);
            return data;
        }
    }

    public void Deserialize(byte[] data, Component instance) {
        var filter = (AudioEchoFilter)instance;
        filter.enabled = false;
        UnitySerializer.AddFinalAction(() => {
            using (new UnitySerializer.SerializationSplitScope()) {
                var si = UnitySerializer.Deserialize<StoredInformation>(data);
                if (si == null) {
                    Debug.LogError("An error occured when getting the stored information for a AudioEchoFilter");
                    return;
                }
                filter.decayRatio = si.decayRatio;
                filter.delay = si.delay;
                filter.wetMix = si.wetMix;
                filter.enabled = si.enabled;
            }
        }
        );
    }
    #endregion
}

[ComponentSerializerFor(typeof(AudioLowPassFilter))]
public class SerializeAudioLowPassFilter : IComponentSerializer {
    public class StoredInformation {
        public bool enabled;
        public float cutoffFrequency;
        public float lowpassResonanceQ;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        using (new UnitySerializer.SerializationSplitScope()) {
            var filter = (AudioLowPassFilter)component;
            var si = new StoredInformation();
            si.enabled = filter.enabled;
            si.cutoffFrequency = filter.cutoffFrequency;
            si.lowpassResonanceQ = filter.lowpassResonanceQ;
            var data = UnitySerializer.Serialize(si);
            return data;
        }
    }

    public void Deserialize(byte[] data, Component instance) {
        var filter = (AudioLowPassFilter)instance;
        filter.enabled = false;
        UnitySerializer.AddFinalAction(() => {
            using (new UnitySerializer.SerializationSplitScope()) {
                var si = UnitySerializer.Deserialize<StoredInformation>(data);
                if (si == null) {
                    Debug.LogError("An error occured when getting the stored information for a AudioLowPassFilter");
                    return;
                }
                filter.cutoffFrequency = si.cutoffFrequency;
                filter.lowpassResonanceQ = si.lowpassResonanceQ;
                filter.enabled = si.enabled;
            }
        }
        );
    }
    #endregion
}

[ComponentSerializerFor(typeof(AudioHighPassFilter))]
public class SerializeAudioHighPassFilter : IComponentSerializer {
    public class StoredInformation {
        public bool enabled;
        public float cutoffFrequency;
        public float highpassResonanceQ;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        using (new UnitySerializer.SerializationSplitScope()) {
            var filter = (AudioHighPassFilter)component;
            var si = new StoredInformation();
            si.enabled = filter.enabled;
            si.cutoffFrequency = filter.cutoffFrequency;
            si.highpassResonanceQ = filter.highpassResonanceQ;
            var data = UnitySerializer.Serialize(si);
            return data;
        }
    }

    public void Deserialize(byte[] data, Component instance) {
        var filter = (AudioHighPassFilter)instance;
        filter.enabled = false;
        UnitySerializer.AddFinalAction(() => {
            using (new UnitySerializer.SerializationSplitScope()) {
                var si = UnitySerializer.Deserialize<StoredInformation>(data);
                if (si == null) {
                    Debug.LogError("An error occured when getting the stored information for a AudioHighPassFilter");
                    return;
                }
                filter.cutoffFrequency = si.cutoffFrequency;
                filter.highpassResonanceQ = si.highpassResonanceQ;
                filter.enabled = si.enabled;
            }
        }
        );
    }
    #endregion
}

[ComponentSerializerFor(typeof(EventSystem))]
public class SerializeEventSystem : IComponentSerializer {
    public class StoredInformation {
        public bool enabled;
        public GameObject firstSelectedGameObject;
        public int pixelDragThreshold;
        public bool sendNavigationEvents;
    }

    #region IComponentSerializer implementation
    public byte[] Serialize(Component component) {
        using (new UnitySerializer.SerializationSplitScope()) {
            var system = (EventSystem)component;
            var si = new StoredInformation();
            si.enabled = system.enabled;
            si.firstSelectedGameObject = system.firstSelectedGameObject;
            si.pixelDragThreshold = system.pixelDragThreshold;
            si.sendNavigationEvents = system.sendNavigationEvents;
            var data = UnitySerializer.Serialize(si);
            return data;
        }
    }

    public void Deserialize(byte[] data, Component instance) {
        var system = (EventSystem)instance;
        system.enabled = false;
        UnitySerializer.AddFinalAction(() => {
            using (new UnitySerializer.SerializationSplitScope()) {
                var si = UnitySerializer.Deserialize<StoredInformation>(data);
                if (si == null) {
                    Debug.LogError("An error occured when getting the stored information for a EventSystem");
                    return;
                }
                system.firstSelectedGameObject = si.firstSelectedGameObject;
                system.pixelDragThreshold = si.pixelDragThreshold;
                system.sendNavigationEvents = si.sendNavigationEvents;
                system.enabled = si.enabled;
            }
        }
        );
    }
    #endregion
}



[SubTypeSerializer(typeof(Component))]
public class SerializeComponentReference : SerializerExtensionBase<Component> {



    public override IEnumerable<object> Save(Component target) {
        //Is this a reference to a prefab
        var assetId = SaveGameManager.Instance.GetAssetId(target);
        if (assetId.index != -1) {
            return new object[] { null, true, target.GetType().FullName, assetId };
        }

        var index = target.gameObject.GetComponents(target.GetType()).FindIndex(c => c == target);

        if (UniqueIdentifier.GetByName(target.gameObject.GetId()) != null) {
            return new object[] { target.gameObject.GetId(), true, target.GetType().FullName, "", index /* Identify a prefab */ };
        }
        else {
            return new object[] { target.gameObject.GetId(), false, target.GetType().FullName, "", index /* Identify a prefab */ };
        }
    }

    public override object Load(object[] data, object instance) {
#if US_LOGGING
		if (!((bool)data [1])) {
			Radical.Log ("[[Disabled, will not be set]]");
		}
		Radical.Log ("Component: {0}.{1}", data [0], data [2]);
#endif

        if (data[3] != null && data[3].GetType() == typeof(SaveGameManager.AssetReference)) {
            return SaveGameManager.Instance.GetAsset((SaveGameManager.AssetReference)data[3]);
        }
        if (data.Length == 5) {
            return new UnitySerializer.DeferredSetter((d) => {
                var item = UniqueIdentifier.GetByName((string)data[0]);
                if (item == null) {
                    Debug.LogError("Could not find reference to " + data[0] + " a " + (string)data[2]);
                    return null;
                }
                var allComponentsOfType = item.GetComponents(UnitySerializer.GetTypeEx(data[2]));
                if (allComponentsOfType.Length == 0)
                    return null;
                if (allComponentsOfType.Length <= (int)data[4])
                    data[4] = 0;
                return item != null ? allComponentsOfType[(int)data[4]] : null;
            }) {
                enabled = (bool)data[1]
            };
        }
        return new UnitySerializer.DeferredSetter((d) => {
            var item = UniqueIdentifier.GetByName((string)data[0]);
            return item != null ? item.GetComponent(UnitySerializer.GetTypeEx(data[2])) : null;
        }) {
            enabled = (bool)data[1]
        };
    }
}

public class ProvideAttributes : IProvideAttributeList {
    private string[] _attributes;
    protected bool AllSimple = true;

    public ProvideAttributes(string[] attributes)
        : this(attributes, true) {
    }

    public ProvideAttributes(string[] attributes, bool allSimple) {
        _attributes = attributes;
        AllSimple = allSimple;
    }

    #region IProvideAttributeList implementation
    public IEnumerable<string> GetAttributeList(Type tp) {
        return _attributes;
    }
    #endregion

    #region IProvideAttributeList implementation
    public virtual bool AllowAllSimple(Type tp) {
        return AllSimple;
    }
    #endregion
}

[AttributeListProvider(typeof(Camera))]
public class ProvideCameraAttributes : ProvideAttributes {
    public ProvideCameraAttributes()
        : base(new string[0]) {
    }
}

[AttributeListProvider(typeof(Transform))]
public class ProviderTransformAttributes : ProvideAttributes {
    public ProviderTransformAttributes()
        : base(new string[] {
		"localPosition",
		"localRotation",
		"localScale"
	}, false) {
    }
}

[AttributeListProvider(typeof(RectTransform))]
public class ProviderRectTransformAttributes : ProvideAttributes {
    public ProviderRectTransformAttributes()
        : base(new string[] {
		"anchoredPosition",
		"anchoredPosition3D",
		"anchorMax",
		"anchorMin",
		"offsetMax",
		"offsetMin",
		"pivot",
        "rect",
        "sizeDelta"
	}, false) {
    }
}

[AttributeListProvider(typeof(Collider))]
public class ProvideColliderAttributes : ProvideAttributes {
    public ProvideColliderAttributes()
        : base(new string[] {
		"material",
		"active",
		"text",
		"mesh",
		"anchor",
		"targetPosition",
		"alignment",
		"lineSpacing",
		"spring",
		"useSpring",
		"motor",
		"useMotor",
		"limits",
		"useLimits",
		"axis",
		"breakForce",
		"breakTorque",
		"connectedBody",
		"offsetZ",
		"playAutomatically",
		"animatePhysics",
		"tabSize",
		"enabled",
		"isTrigger",
		"emit",
		"minSize",
		"maxSize",
		"clip",
		"loop",
		"playOnAwake",
		"bypassEffects",
		"volume",
		"priority",
		"pitch",
		"mute",
		"dopplerLevel",
		"spread",
		"panLevel",
		"volumeRolloff",
		"minDistance",
		"maxDistance",
		"pan2D",
		"castShadows",
		"receiveShadows",
		"slopeLimit",
		"stepOffset",
		"skinWidth",
		"minMoveDistance",
		"center",
		"radius",
		"height",
		"canControl",
		"damper",
		"useFixedUpdate",
		"movement",
		"jumping",
		"movingPlatform",
		"sliding",
		"autoRotate",
		"maxRotationSpeed",
		"range",
		"angle",
		"velocity",
		"intensity",
		"secondaryAxis",
		"xMotion",
		"yMotion",
		"zMotion",
		"angularXMotion",
		"angularYMotion",
		"angularZMotion",
		"linearLimit",
		"lowAngularXLimit",
		"highAngularXLimit",
		"angularYLimit",
		"angularZLimit",
		"targetVelocity",
		"xDrive",
		"yDrive",
		"zDrive",
		"targetAngularVelocity",
		"rotationDriveMode",
		"angularXDrive",
		"angularYZDrive",
		"slerpDrive",
		"projectionMode",
		"projectionDistance",
		"projectionAngle",
		"configuredInWorldSpace",
		"swapBodies",
		"cookie",
		"color",
		"drawHalo",
		"shadowType",
		"renderMode",
		"cullingMask",
		"lightmapping",
		"type",
		"lineSpacing",
		"text",
		"anchor",
		"alignment",
		"tabSize",
		"fontSize",
		"fontStyle",
		"font",
		"characterSize",
		"minEnergy",
		"maxEnergy",
		"minEmission",
		"maxEmission",
		"rndRotation",
		"rndVelocity",
		"rndAngularVelocity",
		"angularVelocity",
		"emitterVelocityScale",
		"localVelocity",
		"worldVelocity",
		"useWorldVelocity"
			
		
	}, false) {

    }
}

[AttributeListProvider(typeof(Renderer))]
[AttributeListProvider(typeof(AudioListener))]
[AttributeListProvider(typeof(ParticleEmitter))]
[AttributeListProvider(typeof(Cloth))]
[AttributeListProvider(typeof(Light))]
[AttributeListProvider(typeof(Joint))]
[AttributeListProvider(typeof(MeshFilter))]
[AttributeListProvider(typeof(TextMesh))]
public class ProviderRendererAttributes : ProvideAttributes {
    public ProviderRendererAttributes()
        : base(new string[] {
		"active",
		"text",
		"anchor",
		"sharedMesh",
		"targetPosition",
		"alignment",
		"lineSpacing",
		"spring",
		"useSpring",
		"motor",
		"useMotor",
		"limits",
		"useLimits",
		"axis",
		"breakForce",
		"breakTorque",
		"connectedBody",
		"offsetZ",
		"playAutomatically",
		"animatePhysics",
		"tabSize",
		"enabled",
		"isTrigger",
		"emit",
		"minSize",
		"maxSize",
		"clip",
		"loop",
		"playOnAwake",
		"bypassEffects",
		"volume",
		"priority",
		"pitch",
		"mute",
		"dopplerLevel",
		"spread",
		"panLevel",
		"volumeRolloff",
		"minDistance",
		"maxDistance",
		"pan2D",
		"castShadows",
		"receiveShadows",
		"slopeLimit",
		"stepOffset",
		"skinWidth",
		"minMoveDistance",
		"center",
		"radius",
		"height",
		"canControl",
		"damper",
		"useFixedUpdate",
		"movement",
		"jumping",
		"movingPlatform",
		"sliding",
		"autoRotate",
		"maxRotationSpeed",
		"range",
		"angle",
		"velocity",
		"intensity",
		"secondaryAxis",
		"xMotion",
		"yMotion",
		"zMotion",
		"angularXMotion",
		"angularYMotion",
		"angularZMotion",
		"linearLimit",
		"lowAngularXLimit",
		"highAngularXLimit",
		"angularYLimit",
		"angularZLimit",
		"targetVelocity",
		"xDrive",
		"yDrive",
		"zDrive",
		"targetAngularVelocity",
		"rotationDriveMode",
		"angularXDrive",
		"angularYZDrive",
		"slerpDrive",
		"projectionMode",
		"projectionDistance",
		"projectionAngle",
		"configuredInWorldSpace",
		"swapBodies",
		"cookie",
		"color",
		"drawHalo",
		"shadowType",
		"renderMode",
		"cullingMask",
		"lightmapping",
		"type",
		"lineSpacing",
		"text",
		"anchor",
		"alignment",
		"tabSize",
		"fontSize",
		"fontStyle",
		"font",
		"characterSize",
		"minEnergy",
		"maxEnergy",
		"minEmission",
		"maxEmission",
		"rndRotation",
		"rndVelocity",
		"rndAngularVelocity",
		"angularVelocity",
		"emitterVelocityScale",
		"localVelocity",
		"worldVelocity",
		"useWorldVelocity"
			
		
	}, false) {

    }
}

