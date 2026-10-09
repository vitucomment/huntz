using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Reviews the real sprite artwork at integer scale before integrating a clip.</summary>
public sealed class HuntzAnimationPreview : EditorWindow
{
    HuntzDirectionalSpriteSet spriteSet;
    int clipIndex;
    int frameIndex;
    bool playing = true;
    double lastTime;
    float elapsed;
    int pixelScale = 2;

    [MenuItem("Huntz/Animation/Preview")]
    public static void Open() => GetWindow<HuntzAnimationPreview>("Huntz Animation");

    void OnEnable()
    {
        spriteSet = Selection.activeObject as HuntzDirectionalSpriteSet;
        if (spriteSet == null)
            spriteSet = AssetDatabase.LoadAssetAtPath<HuntzDirectionalSpriteSet>("Assets/Huntz/Resources/Huntz/Animations/Hero.asset");
        lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    void OnDisable() => EditorApplication.update -= Tick;

    void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        if (playing) elapsed += (float)(now - lastTime);
        lastTime = now;
        if (playing) Repaint();
    }

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        spriteSet = (HuntzDirectionalSpriteSet)EditorGUILayout.ObjectField("Sprite set", spriteSet, typeof(HuntzDirectionalSpriteSet), false);
        if (EditorGUI.EndChangeCheck()) { clipIndex = 0; elapsed = 0f; }
        if (spriteSet == null || spriteSet.Clips == null || spriteSet.Clips.Count == 0) return;
        var clips = spriteSet.Clips;
        clipIndex = Mathf.Clamp(clipIndex, 0, clips.Count - 1);
        EditorGUI.BeginChangeCheck();
        clipIndex = EditorGUILayout.Popup("Action / direction", clipIndex, clips.Select(c => c == null ? "Missing clip" : c.action + " / " + c.direction).ToArray());
        if (EditorGUI.EndChangeCheck()) elapsed = 0f;
        HuntzDirectionalSpriteClip clip = clips[clipIndex];
        if (clip == null || clip.frames == null || clip.frames.Length == 0 || clip.framesPerSecond <= 0f) return;
        frameIndex = Mathf.Clamp(frameIndex, 0, clip.frames.Length - 1);

        using (new EditorGUILayout.HorizontalScope())
        {
            playing = GUILayout.Toggle(playing, "Play", "Button", GUILayout.Width(65));
            if (GUILayout.Button("Restart", GUILayout.Width(65))) elapsed = 0f;
            pixelScale = EditorGUILayout.IntSlider("Pixel scale", pixelScale, 1, 3);
        }
        if (playing)
        {
            float time = clip.loop ? elapsed % clip.Duration : Mathf.Min(elapsed, clip.Duration - 0.0001f);
            frameIndex = Mathf.Clamp(Mathf.FloorToInt(time * clip.framesPerSecond), 0, clip.frames.Length - 1);
        }
        EditorGUI.BeginChangeCheck();
        frameIndex = EditorGUILayout.IntSlider("Frame", frameIndex, 0, clip.frames.Length - 1);
        if (EditorGUI.EndChangeCheck()) { playing = false; elapsed = frameIndex / clip.framesPerSecond; }
        EditorGUILayout.LabelField(clip.framesPerSecond + " fps | " + clip.Duration.ToString("F2") + " s | " + (clip.loop ? "Loop" : "One shot"));
        Sprite sprite = clip.frames[frameIndex];
        if (sprite == null) { EditorGUILayout.HelpBox("Missing sprite reference.", MessageType.Error); return; }
        EditorGUILayout.LabelField(sprite.name);

        Rect area = GUILayoutUtility.GetRect(position.width - 16f, sprite.rect.height * pixelScale + 20f);
        EditorGUI.DrawRect(area, new Color(0.14f, 0.17f, 0.21f));
        Rect target = new Rect(area.center.x - sprite.rect.width * pixelScale * 0.5f, area.y + 10f,
            sprite.rect.width * pixelScale, sprite.rect.height * pixelScale);
        Rect uv = sprite.textureRect;
        uv.x /= sprite.texture.width; uv.width /= sprite.texture.width;
        uv.y /= sprite.texture.height; uv.height /= sprite.texture.height;
        GUI.DrawTextureWithTexCoords(target, sprite.texture, uv, true);
        float ground = target.yMax - sprite.pivot.y * pixelScale;
        EditorGUI.DrawRect(new Rect(area.x, ground, area.width, 1f), new Color(0.4f, 0.8f, 0.7f, 0.5f));
        EditorGUILayout.HelpBox("The line marks the foot pivot. Review alternating contacts, loop continuity and body motion. Different frame names alone do not prove a readable animation.", MessageType.Info);
    }
}

[CustomEditor(typeof(HuntzDirectionalAnimator))]
public sealed class HuntzDirectionalAnimatorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        HuntzDirectionalAnimator animator = (HuntzDirectionalAnimator)target;
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Playing", animator.CurrentClipName ?? "None");
            EditorGUILayout.LabelField("Frame", animator.CurrentFrameIndex.ToString());
            EditorGUILayout.LabelField("Speed", animator.PlaybackSpeed.ToString("F2"));
            Repaint();
        }
        if (GUILayout.Button("Open animation preview"))
        {
            Selection.activeObject = animator.SpriteSet;
            HuntzAnimationPreview.Open();
        }
    }
}
