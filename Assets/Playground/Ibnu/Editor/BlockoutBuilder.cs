using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Taruh file ini di folder Editor. Contoh: Assets/Playground/Ibnu/Editor/BlockoutBuilder.cs
// Script ini memakai Ground yang sudah ada di scene. Tidak membuat Ground atau Wall baru.
public static class BlockoutBuilder
{
    private static readonly string[] IgnoreNames =
    {
        "Ground", "Wall", "Level_Blockout", "Main Camera", "Directional Light",
        "Canvas", "EventSystem", "ObjectiveManager", "NavMesh Surface", "SpawnManager"
    };

    private struct Blocker
    {
        public Vector3 center;
        public float radius;
    }

    [MenuItem("Tools/Build Blockout Level")]
    public static void Build()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            Debug.LogError("Objek bernama Ground tidak ditemukan di Hierarchy.");
            return;
        }

        Bounds b;
        Collider col = ground.GetComponentInChildren<Collider>();
        Renderer rend = ground.GetComponentInChildren<Renderer>();
        if (col != null) b = col.bounds;
        else if (rend != null) b = rend.bounds;
        else
        {
            Debug.LogError("Ground tidak punya Collider atau Renderer.");
            return;
        }

        GameObject old = GameObject.Find("Level_Blockout");
        if (old != null)
            Undo.DestroyObjectImmediate(old);

        // Catat semua objek yang sudah ada supaya tidak ditimpa.
        List<Blocker> blockers = CollectExistingObjects();

        GameObject level = new GameObject("Level_Blockout");
        Undo.RegisterCreatedObjectUndo(level, "Build Blockout");

        float top = b.max.y;
        float margin = 3f;

        // Zona tengah. 20 persen kiri dan kanan dikosongkan untuk spawn dan altar.
        float minX = b.min.x + b.size.x * 0.2f;
        float maxX = b.max.x - b.size.x * 0.2f;
        float minZ = b.min.z + margin;
        float maxZ = b.max.z - margin;

        Random.InitState(System.Environment.TickCount);
        List<Vector3> used = new List<Vector3>();

        GameObject props = new GameObject("Props");
        props.transform.SetParent(level.transform);

        for (int i = 1; i <= 10; i++)
        {
            Vector3 p = FindSpot(used, blockers, minX, maxX, minZ, maxZ, top, 1f);
            used.Add(p);
            MakeTree(props.transform, "Blockout_Tree_" + i.ToString("00"), p);
        }

        for (int i = 1; i <= 6; i++)
        {
            Vector3 p = FindSpot(used, blockers, minX, maxX, minZ, maxZ, top, 1.5f);
            used.Add(p);
            MakeRock(props.transform, "Blockout_Rock_" + i.ToString("00"), p);
        }

        GameObject urns = new GameObject("UrnSpawnPoints");
        urns.transform.SetParent(level.transform);
        for (int i = 1; i <= 4; i++)
        {
            Vector3 p = FindSpot(used, blockers, minX, maxX, minZ, maxZ, top, 1f);
            used.Add(p);
            MakeEmpty(urns.transform, "UrnSpawn_" + i.ToString("00"), p);
        }

        GameObject enemies = new GameObject("EnemySpawnPoints");
        enemies.transform.SetParent(level.transform);
        for (int i = 1; i <= 4; i++)
        {
            Vector3 p = FindSpot(used, blockers, minX, maxX, minZ, maxZ, top, 1f);
            used.Add(p);
            MakeEmpty(enemies.transform, "EnemySpawn_" + i.ToString("00"), p);
        }

        Selection.activeGameObject = level;
    }

    private static List<Blocker> CollectExistingObjects()
    {
        List<Blocker> list = new List<Blocker>();
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject go in roots)
        {
            if (!go.activeInHierarchy) continue;

            bool skip = false;
            foreach (string n in IgnoreNames)
            {
                if (go.name == n)
                {
                    skip = true;
                    break;
                }
            }
            if (skip) continue;

            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            Collider[] cs = go.GetComponentsInChildren<Collider>();
            if (rs.Length == 0 && cs.Length == 0) continue;

            Bounds ob = new Bounds(go.transform.position, Vector3.zero);
            foreach (Renderer r in rs) ob.Encapsulate(r.bounds);
            foreach (Collider c in cs) ob.Encapsulate(c.bounds);

            Blocker bl = new Blocker();
            bl.center = ob.center;
            bl.radius = Mathf.Max(ob.extents.x, ob.extents.z);
            list.Add(bl);
        }
        return list;
    }

    private static Vector3 FindSpot(List<Vector3> used, List<Blocker> blockers,
        float minX, float maxX, float minZ, float maxZ, float y, float ownRadius)
    {
        float gap = 5f;
        float pad = 2.5f;
        Vector3 p = Vector3.zero;

        for (int attempt = 0; attempt < 400; attempt++)
        {
            // Longgarkan aturan kalau ruang terlalu sempit.
            if (attempt == 200) { gap = 3f; pad = 1.5f; }

            p = new Vector3(Random.Range(minX, maxX), y, Random.Range(minZ, maxZ));

            bool ok = true;

            foreach (Vector3 u in used)
            {
                if (Vector3.Distance(p, u) < gap)
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                foreach (Blocker bl in blockers)
                {
                    Vector2 a = new Vector2(p.x, p.z);
                    Vector2 c = new Vector2(bl.center.x, bl.center.z);
                    if (Vector2.Distance(a, c) < bl.radius + ownRadius + pad)
                    {
                        ok = false;
                        break;
                    }
                }
            }

            if (ok) break;
        }
        return p;
    }

    private static void MakeProp(Transform parent, string name, Vector3 pos,
        PrimitiveType type, Vector3 size, Color color, bool capsule)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(pos.x, pos.y + size.y / 2f, pos.z);

        if (capsule)
        {
            CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
            cc.radius = size.x / 2f;
            cc.height = size.y;
        }
        else
        {
            BoxCollider bc = root.AddComponent<BoxCollider>();
            bc.size = size;
        }

        GameObject visual = GameObject.CreatePrimitive(type);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;

        // Silinder Unity tingginya 2 unit, jadi dibagi dua agar sama dengan collider.
        Vector3 s = size;
        if (type == PrimitiveType.Cylinder)
            s = new Vector3(size.x, size.y / 2f, size.z);
        visual.transform.localScale = s;

        Renderer r = visual.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        r.sharedMaterial = mat;
    }

    // Pohon 2D: collider 3D di induk, SpriteRenderer di child Visual.
    private static void MakeTree(Transform parent, string name, Vector3 pos)
    {
        Vector3 size = new Vector3(1f, 3f, 1f);

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(pos.x, pos.y + size.y / 2f, pos.z);

        CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
        cc.radius = 0.4f;
        cc.height = size.y;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        // Pivot sprite di bawah, jadi turunkan ke dasar collider.
        visual.transform.localPosition = new Vector3(0f, -size.y / 2f, 0f);

        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = GetTreeSprite();

        // Hadapkan sprite ke kamera.
        Camera cam = Camera.main;
        if (cam != null)
            visual.transform.rotation = cam.transform.rotation;

        // Pakai script Billboard milikmu kalau ada.
        System.Type billboard = System.Type.GetType("Billboard, Assembly-CSharp");
        if (billboard != null && typeof(Component).IsAssignableFrom(billboard))
            visual.AddComponent(billboard);
    }

    // Membuat PNG pohon sederhana satu kali. Tim bisa menimpa file ini dengan art final.
    private static Sprite GetTreeSprite()
    {
        string folder = "Assets/Blockout";
        string path = folder + "/Blockout_Tree.png";

        if (!System.IO.File.Exists(Application.dataPath + "/Blockout/Blockout_Tree.png"))
        {
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "Blockout");

            int w = 128, h = 192;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color trunk = new Color(0.35f, 0.22f, 0.1f, 1f);
            Color leaf = new Color(0.5f, 0.34f, 0.18f, 1f);
            Color leafDark = new Color(0.42f, 0.27f, 0.13f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = clear;
                    if (x >= 52 && x <= 76 && y <= 100)
                        c = trunk;

                    float dx = x - 64f;
                    float dy = y - 140f;
                    if (dx * dx + dy * dy <= 52f * 52f)
                        c = (dy < -10f) ? leafDark : leaf;

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            System.IO.File.WriteAllBytes(Application.dataPath + "/Blockout/Blockout_Tree.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 64f;
                imp.alphaIsTransparency = true;

                TextureImporterSettings s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);
                s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                imp.SetTextureSettings(s);
                imp.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Batu 2D: collider 3D di induk, SpriteRenderer di child Visual.
    private static void MakeRock(Transform parent, string name, Vector3 pos)
    {
        Vector3 size = new Vector3(2f, 1.5f, 2f);

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(pos.x, pos.y + size.y / 2f, pos.z);

        BoxCollider bc = root.AddComponent<BoxCollider>();
        bc.size = size;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, -size.y / 2f, 0f);

        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = GetRockSprite();

        Camera cam = Camera.main;
        if (cam != null)
            visual.transform.rotation = cam.transform.rotation;

        System.Type billboard = System.Type.GetType("Billboard, Assembly-CSharp");
        if (billboard != null && typeof(Component).IsAssignableFrom(billboard))
            visual.AddComponent(billboard);
    }

    // Membuat PNG batu sederhana satu kali. Tim bisa menimpa file ini dengan art final.
    private static Sprite GetRockSprite()
    {
        string folder = "Assets/Blockout";
        string path = folder + "/Blockout_Rock.png";

        if (!System.IO.File.Exists(Application.dataPath + "/Blockout/Blockout_Rock.png"))
        {
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "Blockout");

            int w = 128, h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color top = new Color(0.55f, 0.55f, 0.55f, 1f);
            Color bottom = new Color(0.4f, 0.4f, 0.4f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - 64f) / 62f;
                    float ny = y / 90f;
                    Color c = clear;
                    if (nx * nx + ny * ny <= 1f)
                        c = (y < 35) ? bottom : top;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            System.IO.File.WriteAllBytes(Application.dataPath + "/Blockout/Blockout_Rock.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 64f;
                imp.alphaIsTransparency = true;

                TextureImporterSettings s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);
                s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                imp.SetTextureSettings(s);
                imp.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void MakeEmpty(Transform parent, string name, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
    }
}