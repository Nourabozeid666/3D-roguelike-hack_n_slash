using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static float Distance(Vector3 a, Vector3 b) => (float)Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z));
        public override string ToString() => $"({x}, {y}, {z})";
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public override string ToString() => $"({x}, {y})";
    }

    public struct Quaternion
    {
        public static Quaternion identity => new Quaternion();
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color red => new Color(1, 0, 0, 1);
        public static Color yellow => new Color(1, 1, 0, 1);
        public static Color white => new Color(1, 1, 1, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color cyan => new Color(0, 1, 1, 1);
    }

    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public class Font
    {
        public string name;
        public Font() { }
        public Font(string name) { this.name = name; }
    }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : new() => new T();
    }

    public class Canvas : Component
    {
        public RenderMode renderMode;
        public int sortingOrder;
    }

    public class RectTransform : Component
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
    }

    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }

    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    public class Shader
    {
        public static Shader Find(string name) => new Shader();
    }

    public class Material
    {
        public Shader shader;
        public Color color;
        public Material(Shader shader) { this.shader = shader; }
    }

    public enum PrimitiveType { Sphere, Capsule, Cube }

    public class Gizmos
    {
        public static Color color;
        public static void DrawWireSphere(Vector3 c, float r) { }
        public static void DrawWireCube(Vector3 center, Vector3 size) { }
    }

    public static class Mathf
    {
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static bool Approximately(float a, float b) => Math.Abs(a - b) < 0.0001f;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static float Min(float a, float b) => a < b ? a : b;
    }

    public static class Random
    {
        static int seq;
        public static int Range(int min, int max)
        {
            if (max <= min) return min;
            seq = unchecked(seq * 1103515245 + 12345);
            return min + (seq & 0x7fffffff) % (max - min);
        }
        // Deterministic LCG float so RandomZone candidates vary predictably (same seed sequence
        // across runs). min + [0,1)*range.
        public static float Range(float min, float max)
        {
            if (max <= min) return min;
            seq = unchecked(seq * 1103515245 + 12345);
            return min + ((seq & 0x7fffffff) % 1000000) / 1000000f * (max - min);
        }
    }

    public static class Debug
    {
        public static void Log(object m) { }
        public static void LogWarning(object m) { }
    }

    public static class Time
    {
        public static float time;
        public static float timeScale = 1f;
    }

    public static class Application
    {
        public static string persistentDataPath = Path.Combine(Path.GetTempPath(), "opencode", "persistent_data");
        public static void Quit() { }
    }

    public static class JsonUtility
    {
        public static string ToJson(object obj, bool prettyPrint)
            => JsonSerializer.Serialize(obj, obj.GetType(), new JsonSerializerOptions { IncludeFields = true, WriteIndented = prettyPrint });

        public static T FromJson<T>(string json)
            => JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { IncludeFields = true });
    }

    public enum CursorLockMode { None, Locked, Confined }

    public static class Cursor
    {
        public static bool visible = true;
        public static CursorLockMode lockState = CursorLockMode.None;
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
    }

    public static class GUI
    {
        public static void Box(Rect position, string text) { }
    }

    public class Object
    {
        public static readonly List<GameObject> All = new();
        public static readonly List<GameObject> Clones = new();

        public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation)
        {
            original.isPrefab = true;
            var clone = new GameObject(original.name);
            clone.transform.position = position;
            clone.transform.rotation = rotation;
            clone.tag = original.tag;
            clone.layer = original.layer;

            foreach (Component comp in original.components.ToList())
            {
                if (comp is Transform) continue;
                Component copy = clone.AddComponent(comp.GetType());
                foreach (FieldInfo f in comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (f.DeclaringType == comp.GetType())
                        f.SetValue(copy, f.GetValue(comp));
                }
            }

            foreach (Component mb in clone.components.ToList())
            {
                MethodInfo awake = mb.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                awake?.Invoke(mb, null);
            }

            Clones.Add(clone);
            return clone;
        }

        public static void Destroy(Object obj)
        {
            (obj as GameObject)?.MarkDestroyed();
        }

        public static T[] FindObjectsOfType<T>() where T : Component
        {
            var result = new List<T>();
            foreach (GameObject go in All)
            {
                if (go.IsDestroyed || go.isPrefab) continue;
                foreach (Component c in go.components)
                    if (c is T typed) result.Add(typed);
            }
            return result.ToArray();
        }

        public static T FindObjectOfType<T>() where T : Component
        {
            T[] all = FindObjectsOfType<T>();
            return all.Length > 0 ? all[0] : null;
        }

        public static T FindFirstObjectByType<T>() where T : Component => FindObjectOfType<T>();

        public static void ResetWorld()
        {
            foreach (GameObject go in All) go.MarkDestroyed();
            All.Clear();
            Clones.Clear();
        }
    }

    namespace SceneManagement
    {
        public struct Scene
        {
            public string name;
        }

        public static class SceneManager
        {
            public static string activeSceneName = "TestingScene";
            public static string lastLoadedScene;
            public static Scene GetActiveScene() => new Scene { name = activeSceneName };
            public static void LoadScene(string sceneName) => lastLoadedScene = sceneName;
        }
    }

    public class GameObject : Object
    {
        public string name;
        public string tag = "Untagged";
        public int layer;
        bool active = true;
        bool destroyed;

        public readonly Transform transform;
        public readonly List<Component> components = new();

        public GameObject(string name)
        {
            this.name = name;
            transform = new Transform();
            transform.gameObject = this;
            transform.transform = transform;
            transform.localPosition = Vector3.zero;
            components.Add(transform);
            Object.All.Add(this);
        }

        public bool activeSelf => active && !destroyed;
        public bool IsDestroyed => destroyed;
        public bool isPrefab;
        public void SetActive(bool a) => active = a;
        public void MarkDestroyed() => destroyed = true;

        public T GetComponent<T>() => components.OfType<T>().FirstOrDefault();
        public Component GetComponent(Type t) => components.FirstOrDefault(c => t.IsInstanceOfType(c));

        public T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            var result = new List<T>();
            Collect<T>(transform, result);
            return result.ToArray();
        }

        public T GetComponentInChildren<T>() where T : Component
        {
            T[] all = GetComponentsInChildren<T>(true);
            return all.Length > 0 ? all[0] : null;
        }

        void Collect<T>(Transform t, List<T> result)
        {
            foreach (Component c in t.gameObject.components)
                if (c is T typed) result.Add(typed);
            foreach (Transform child in t.children)
                Collect<T>(child, result);
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var c = new T();
            c.gameObject = this;
            c.transform = transform;
            components.Add(c);
            return c;
        }

        public Component AddComponent(Type t)
        {
            var c = (Component)Activator.CreateInstance(t);
            c.gameObject = this;
            c.transform = transform;
            components.Add(c);
            return c;
        }

        public bool CompareTag(string t) => tag == t;

        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            var go = new GameObject(type.ToString());
            go.AddComponent<CapsuleCollider>();
            go.AddComponent<MeshRenderer>();
            return go;
        }
    }

    public abstract class ScriptableObject { }

    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform;
        public string name => gameObject != null ? gameObject.name : "";

        public T GetComponent<T>() => gameObject.GetComponent<T>();
        public Component GetComponent(Type t) => gameObject.GetComponent(t);
        public T GetComponentInChildren<T>() where T : Component => gameObject.GetComponentInChildren<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) => gameObject.GetComponentsInChildren<T>(includeInactive);
        public bool CompareTag(string t) => gameObject.CompareTag(t);

        public static implicit operator bool(Component c) => c != null;
    }

    public class MonoBehaviour : Component
    {
        /// <summary>Deferred coroutine queue so tests can pump Unity's coroutine timing manually
        /// (StartCoroutine queues; RunPendingCoroutines drains it at a "frame" boundary).</summary>
        public static readonly List<IEnumerator> PendingCoroutines = new();

        public Coroutine StartCoroutine(IEnumerator routine)
        {
            PendingCoroutines.Add(routine);
            return new Coroutine();
        }

        public static void RunPendingCoroutines()
        {
            int guard = 0;
            while (PendingCoroutines.Count > 0 && guard++ < 10000)
            {
                IEnumerator routine = PendingCoroutines[0];
                PendingCoroutines.RemoveAt(0);
                while (routine.MoveNext()) { }
            }
        }
    }

    public class Coroutine { }

    public class WaitForSeconds
    {
        public float seconds;
        public WaitForSeconds(float seconds) { this.seconds = seconds; }
    }

    public class Transform : Component
    {
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 localScale { get; set; } = new Vector3(1, 1, 1);
        public Transform parent { get; private set; }
        public readonly List<Transform> children = new();

        public Vector3 position
        {
            get => parent != null ? parent.position + localPosition : localPosition;
            set => localPosition = parent != null ? value - parent.position : value;
        }

        public int childCount => children.Count;

        public Transform GetChild(int index) => children[index];

        public void SetParent(Transform newParent, bool worldPositionStays)
        {
            if (parent != null) parent.children.Remove(this);
            parent = newParent;
            newParent?.children.Add(this);
        }
    }

    public abstract class Collider : Component
    {
        public bool isTrigger;
        public bool enabled = true;
    }

    public class CapsuleCollider : Collider
    {
        public float radius = 0.5f;
        public float height = 2f;
        public Vector3 center;
        public int direction = 1;
    }

    public class MeshRenderer : Component
    {
        public Material sharedMaterial;
    }

    public class MeshFilter : Component { }

    /// <summary>Layer bitmask. Layer numbers are never hardcoded in production code — the value is
    /// set in the Inspector / tests; the stub just carries it (the real mask gates physics).</summary>
    public struct LayerMask
    {
        public int value;

        public static implicit operator int(LayerMask m) => m.value;
        public static implicit operator LayerMask(int i) => new LayerMask { value = i };
    }

    public struct Bounds
    {
        public Vector3 center;
        public Vector3 size;

        public bool Contains(Vector3 p)
        {
            Vector3 min = new Vector3(center.x - size.x * 0.5f, center.y - size.y * 0.5f, center.z - size.z * 0.5f);
            Vector3 max = new Vector3(center.x + size.x * 0.5f, center.y + size.y * 0.5f, center.z + size.z * 0.5f);
            return p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y && p.z >= min.z && p.z <= max.z;
        }
    }

    /// <summary>Deterministic Physics fake. No colliders exist in the harness; tests register blocked
    /// positions (with a radius) that Physics.CheckSphere treats as blocking geometry.</summary>
    public static class Physics
    {
        public static readonly List<Vector3> BlockedPositions = new();
        public static float BlockedRadius = 0.5f;

        public static void ResetBlockers()
        {
            BlockedPositions.Clear();
            BlockedRadius = 0.5f;
        }

        public static bool CheckSphere(Vector3 center, float radius, int layerMask)
        {
            foreach (Vector3 b in BlockedPositions)
                if (Vector3.Distance(center, b) <= radius + BlockedRadius) return true;
            return false;
        }
    }

    public class CreateAssetMenuAttribute : Attribute
    {
        public string fileName;
        public string menuName;
    }

    public class SerializeFieldAttribute : Attribute { }

    public class TooltipAttribute : Attribute
    {
        public string tooltip;
        public TooltipAttribute(string tooltip) { this.tooltip = tooltip; }
    }

    namespace AI
    {
        public struct NavMeshHit
        {
            public Vector3 position;
            public bool hit;
        }

        /// <summary>Deterministic NavMesh fake. With FakeValid true every candidate is walkable and
        /// snaps to itself; with FakeValid false only positions inside FakeArea are walkable. Tests
        /// use this to exercise the "no baked NavMesh / invalid geometry" paths without Unity.</summary>
        public static class NavMesh
        {
            public const int AllAreas = -1;

            public static bool FakeValid = true;
            public static Bounds FakeArea;

            public static bool SamplePosition(Vector3 source, out NavMeshHit hit, float maxDistance, int areas)
            {
                hit = new NavMeshHit { position = source, hit = FakeValid };
                if (FakeValid) return true;
                return FakeArea.Contains(source);
            }
        }
    }
}

namespace UnityEngine.UI
{
    public class CanvasScaler : Component
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public ScaleMode uiScaleMode;
        public Vector2 referenceResolution;
        public float matchWidthOrHeight;
    }

    public class Graphic : Component
    {
        public Color color;
        public bool raycastTarget;
    }

    public class Image : Graphic
    {
    }

    public class Text : Graphic
    {
        public Font font;
        public int fontSize;
        public FontStyle fontStyle;
        public TextAnchor alignment;
        public HorizontalWrapMode horizontalOverflow;
        public VerticalWrapMode verticalOverflow;
        public string text;
    }

    public class ButtonClickedEvent
    {
        public readonly List<Action> Actions = new();
        public void AddListener(Action action) => Actions.Add(action);
        public void Invoke()
        {
            foreach (Action a in Actions.ToList()) a();
        }
    }

    public class Button : Graphic
    {
        public bool interactable = true;
        public ButtonClickedEvent onClick = new();
    }
}
