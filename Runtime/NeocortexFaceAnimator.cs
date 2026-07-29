using UnityEngine;
using Neocortex.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Neocortex
{
    /// <summary>
    ///     Lip-syncs chat replies on any ARKit-compatible face (Ready Player Me included). Put it on
    ///     or above a face mesh; with a <see cref="NeocortexSmartAgent"/> assigned or found on this
    ///     object it animates spoken replies automatically, otherwise call <see cref="PlayText"/>
    ///     yourself. Blendshape names match case-insensitively and ignore importer prefixes.
    /// </summary>
    [AddComponentMenu("Neocortex/Neocortex Face Animator", 0)]
    public class NeocortexFaceAnimator : MonoBehaviour
    {
        [Tooltip("Drives lip-sync from this agent's chat lines. Auto-resolved from this object and its parents when empty.")]
        [SerializeField] private NeocortexSmartAgent agent;

        [Tooltip("The source the voice plays through, the mouth stops the moment it stops. Auto-resolved from the agent when empty.")]
        [SerializeField] private AudioSource audioSource;
        
        [Tooltip("Face meshes with blendshapes. Auto-resolved from children when empty.")]
        [SerializeField] private SkinnedMeshRenderer[] meshes;

        // The viseme table (weights 0..1), same values as the web mascot.
        private static readonly Dictionary<string, (string shape, float weight)[]> Visemes = new()
        {
            ["A"] = new[] { ("JawOpen", 0.4f), ("MouthLowerDownLeft", 0.3f), ("MouthLowerDownRight", 0.3f) },
            ["E"] = new[] { ("JawOpen", 0.15f), ("MouthSmileLeft", 0.3f), ("MouthSmileRight", 0.3f), ("MouthStretchLeft", 0.4f), ("MouthStretchRight", 0.4f) },
            ["I"] = new[] { ("JawOpen", 0.1f), ("MouthSmileLeft", 0.4f), ("MouthSmileRight", 0.4f), ("MouthDimpleLeft", 0.2f), ("MouthDimpleRight", 0.2f) },
            ["O"] = new[] { ("JawOpen", 0.3f), ("MouthFunnel", 0.8f) },
            ["U"] = new[] { ("JawOpen", 0.15f), ("MouthPucker", 0.8f) },
            ["M_B_P"] = new[] { ("MouthClose", 0.2f), ("MouthPressLeft", 0.2f), ("MouthPressRight", 0.2f), ("MouthRollLower", 0.1f), ("MouthRollUpper", 0.1f) },
            ["F_V"] = new[] { ("JawOpen", 0.1f), ("MouthRollLower", 0.6f), ("MouthShrugUpper", 0.2f) },
            ["CH_J_SH"] = new[] { ("JawOpen", 0.1f), ("MouthFunnel", 0.3f), ("MouthShrugUpper", 0.2f), ("MouthShrugLower", 0.2f) },
            ["W_Q"] = new[] { ("JawOpen", 0.1f), ("MouthPucker", 0.9f) },
            ["L_N_T_D"] = new[] { ("MouthStretchLeft", 0.3f), ("MouthStretchRight", 0.3f) },
            ["R"] = new[] { ("JawOpen", 0.1f), ("MouthFunnel", 0.2f), ("MouthPucker", 0.2f), ("MouthStretchLeft", 0.1f), ("MouthStretchRight", 0.1f) },
            ["REST"] = new (string, float)[0],
        };

        private static readonly Regex WordPattern = new("[a-z]+", RegexOptions.Compiled);
        private static readonly Regex SyllablePattern = new("[^aeiouy]*[aeiouy]+(?:[^aeiouy]*$|[^aeiouy](?=[^aeiouy]))?", RegexOptions.Compiled);

        // Per mesh: canonical shape name (lowercase, prefix-stripped) → blendshape index, plus a
        // full-length target-weight array that Update() lerps the real weights toward, and each
        // shape's full frame weight (100 on FBX imports, 1 on glTF imports) so viseme weights
        // scale correctly regardless of importer.
        private Dictionary<string, int>[] shapeIndices;
        private float[][] targetWeights;
        private float[][] fullWeights;

        // The reply text currently being spoken, so an incoming audio clip can be paired with it.
        private string lastReplyText = "";
        private string lastLineText = "";

        private bool babbling;
        private float timer;
        private float currentDuration;
        private readonly List<(string viseme, float duration)> sequence = new();
        private int sequenceIndex;

        // Audio gate: when the animation was started by a clip, the mouth is hard-stopped the
        // moment the source stops playing, clip.length can over-report (padded MP3s), and the
        // sequence must never outlive the audible voice.
        private bool audioDriven;
        private bool waitingForAudioStart;

        private void Awake()
        {
            if (agent == null)
            {
                agent = GetComponentInParent<NeocortexSmartAgent>();
            }

            if (meshes == null || meshes.Length == 0)
            {
                meshes = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            }

            BuildShapeLookup();

            if (agent != null)
            {
                // Lip-sync is driven by the ACTUAL audio: no clip, no face animation. The clip is
                // paired with the text it voices (the whole reply in Single Audio / Off modes, the
                // current line in Per-Line Audio).
                agent.OnChatResponseReceived.AddListener(response => lastReplyText = response.message);
                agent.OnChatLineStarted.AddListener(line => lastLineText = line.text);
                agent.OnAudioResponseReceived.AddListener(HandleAudioClip);
                agent.OnReplyFinished.AddListener(Rest);
            }
        }

        private void HandleAudioClip(AudioClip clip)
        {
            if (clip == null) return;

            // The agent resolves its AudioSource in its own Awake, so resolve lazily here.
            if (audioSource == null)
            {
                audioSource = agent.audioSource;
            }

            string text = agent.chatLinesMode == ChatLinesMode.PerLineAudio ? lastLineText : lastReplyText;
            PlayText(text ?? "", clip.length);

            audioDriven = true;
            waitingForAudioStart = true; // the clip starts playing right after this event
        }

        private void BuildShapeLookup()
        {
            var validMeshes = new List<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer mesh in meshes)
            {
                if (mesh != null && mesh.sharedMesh != null && mesh.sharedMesh.blendShapeCount > 0)
                {
                    validMeshes.Add(mesh);
                }
            }
            meshes = validMeshes.ToArray();

            shapeIndices = new Dictionary<string, int>[meshes.Length];
            targetWeights = new float[meshes.Length][];
            fullWeights = new float[meshes.Length][];

            for (int m = 0; m < meshes.Length; m++)
            {
                Mesh mesh = meshes[m].sharedMesh;
                shapeIndices[m] = new Dictionary<string, int>();
                targetWeights[m] = new float[mesh.blendShapeCount];
                fullWeights[m] = new float[mesh.blendShapeCount];

                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    // "Face.JawOpen" / "blendShape1.jawOpen" / "JawOpen" → "jawopen"
                    string raw = mesh.GetBlendShapeName(i);
                    int dot = raw.LastIndexOf('.');
                    string canonical = (dot >= 0 ? raw.Substring(dot + 1) : raw).ToLowerInvariant();
                    shapeIndices[m][canonical] = i;

                    // The shape's "fully applied" weight comes from its last frame: FBX imports
                    // use 100, glTF imports use 1. Read it instead of assuming either.
                    float full = mesh.GetBlendShapeFrameWeight(i, mesh.GetBlendShapeFrameCount(i) - 1);
                    fullWeights[m][i] = full > 0 ? full : 100f;
                }
            }
        }

        /// <summary>Lip-syncs text across a duration. The agent calls this for every chat line.</summary>
        /// <param name="text">What the character is saying.</param>
        /// <param name="duration">Seconds to spread the visemes over, usually the clip's length.</param>
        public void PlayText(string text, float duration)
        {
            babbling = false;
            audioDriven = false;
            sequence.Clear();

            var generated = new List<(string viseme, int weight)>();
            foreach (Match word in WordPattern.Matches(text.ToLowerInvariant()))
            {
                foreach (Match syllable in SyllablePattern.Matches(word.Value))
                {
                    foreach (char c in syllable.Value)
                    {
                        string v = VisemeFor(c);
                        if (v != null)
                        {
                            generated.Add((v, "aeiouy".IndexOf(c) >= 0 ? 3 : 1));
                        }
                    }
                }

                generated.Add(("REST", 2));
            }

            if (generated.Count == 0)
            {
                if (duration > 0) StartSpeaking();
                return;
            }

            int totalWeight = 0;
            foreach ((_, int weight) in generated) totalWeight += weight;
            float durationPerWeight = duration / totalWeight;

            foreach ((string viseme, int weight) in generated)
            {
                sequence.Add((viseme, weight * durationPerWeight));
            }

            sequenceIndex = 0;
            timer = 0;
            currentDuration = sequence[0].duration;
            ApplyViseme(sequence[0].viseme);
        }

        /// <summary>Random mouth movement with no text, an "is talking" fallback.</summary>
        public void StartSpeaking()
        {
            babbling = true;
            audioDriven = false;
            sequence.Clear();
            PickRandomViseme();
        }

        /// <summary>Back to a neutral face.</summary>
        public void Rest()
        {
            babbling = false;
            audioDriven = false;
            sequence.Clear();
            ApplyViseme("REST");
        }

        /// <summary>Holds one blendshape as an expression, replacing any running lip-sync.</summary>
        public void PlayExpression(string blendShapeName, float weight = 1f)
        {
            babbling = false;
            audioDriven = false;
            sequence.Clear();
            ApplyShapes(new[] { (blendShapeName, weight) });
        }

        private static string VisemeFor(char c)
        {
            if (c == 'a') return "A";
            if (c == 'e') return "E";
            if (c == 'i' || c == 'y') return "I";
            if (c == 'o') return "O";
            if (c == 'u') return "U";
            if (c == 'm' || c == 'b' || c == 'p') return "M_B_P";
            if (c == 'f' || c == 'v') return "F_V";
            if (c == 'c' || c == 'j' || c == 'q' || c == 'x' || c == 's' || c == 'h') return "CH_J_SH";
            if (c == 'w' || c == 'z') return "W_Q";
            if (c == 'l' || c == 'n' || c == 't' || c == 'd') return "L_N_T_D";
            if (c == 'r' || c == 'g') return "R";
            return null;
        }

        private void PickRandomViseme()
        {
            var keys = new List<string>(Visemes.Keys);
            ApplyViseme(keys[Random.Range(0, keys.Count)]);
            timer = 0;
            currentDuration = Random.Range(0.05f, 0.2f);
        }

        private void ApplyViseme(string name)
        {
            ApplyShapes(Visemes.TryGetValue(name, out var shapes) ? shapes : Visemes["REST"]);
        }

        private void ApplyShapes((string shape, float weight)[] shapes)
        {
            for (int m = 0; m < meshes.Length; m++)
            {
                float[] targets = targetWeights[m];
                for (int i = 0; i < targets.Length; i++) targets[i] = 0;

                foreach ((string shape, float weight) in shapes)
                {
                    if (shapeIndices[m].TryGetValue(shape.ToLowerInvariant(), out int index))
                    {
                        // Scale by the shape's real full-frame weight (100 for FBX, 1 for glTF).
                        targets[index] = weight * fullWeights[m][index];
                    }
                }
            }
        }

        private void Update()
        {
            // Audio gate: once the clip has been heard playing, the mouth may not outlive it.
            if (audioDriven && audioSource != null)
            {
                if (waitingForAudioStart)
                {
                    if (audioSource.isPlaying)
                    {
                        waitingForAudioStart = false;
                    }
                }
                else if (!audioSource.isPlaying && (babbling || sequence.Count > 0))
                {
                    Rest();
                }
            }

            if (babbling)
            {
                timer += Time.deltaTime;
                if (timer >= currentDuration)
                {
                    PickRandomViseme();
                }
            }
            else if (sequence.Count > 0)
            {
                timer += Time.deltaTime;
                if (timer >= currentDuration)
                {
                    sequenceIndex++;
                    if (sequenceIndex < sequence.Count)
                    {
                        (string viseme, float duration) = sequence[sequenceIndex];
                        ApplyViseme(viseme);
                        timer -= currentDuration; // keep exactly in sync
                        currentDuration = duration;
                    }
                    else
                    {
                        sequence.Clear();
                        ApplyViseme("REST");
                    }
                }
            }

            // Smoothly interpolate toward the targets; alpha capped so a background-tab hitch
            // can't overshoot.
            float alpha = Mathf.Min(1f, Time.deltaTime * 10);
            for (int m = 0; m < meshes.Length; m++)
            {
                SkinnedMeshRenderer mesh = meshes[m];
                float[] targets = targetWeights[m];
                for (int i = 0; i < targets.Length; i++)
                {
                    float current = mesh.GetBlendShapeWeight(i);
                    float next = Mathf.Clamp(current + (targets[i] - current) * alpha, 0f, fullWeights[m][i]);
                    mesh.SetBlendShapeWeight(i, next);
                }
            }
        }
    }
}
