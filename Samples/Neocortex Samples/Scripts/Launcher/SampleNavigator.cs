using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Neocortex.Samples
{
    /// <summary>
    ///     The Back button canvas in the launcher scene. Inactive there; the launcher carries it into
    ///     the sample it opens. It gives the sample the picked character, and its Back button (or
    ///     Escape) returns to the list, so the samples themselves stay free of navigation code.
    /// </summary>
    public class SampleNavigator : MonoBehaviour
    {
        private const string CharacterKey = "Neocortex.Samples.CharacterId";
        private const string OthersKey = "Neocortex.Samples.OtherCharacterIds";
        private const string NameKey = "Neocortex.Samples.CharacterName";
        private const string QuestionSetKey = "Neocortex.Samples.QuestionSetId";

        [SerializeField] private Button backButton;

        private string launcherPath;

        /// <summary>The character the samples use, or "" before the launcher has loaded your team.</summary>
        public static string SavedCharacterId => PlayerPrefs.GetString(CharacterKey, "");

        /// <summary>The question set the quiz sample plays, or "".</summary>
        public static string SavedQuestionSetId => PlayerPrefs.GetString(QuestionSetKey, "");

        /// <summary>Remembers the picked character and the team's other characters, for the group sample.</summary>
        /// <param name="characterId">The character every sample uses.</param>
        /// <param name="characterName">Its name, for the quiz host's bubbles.</param>
        /// <param name="otherIds">The team's other characters, in list order.</param>
        public static void SaveCharacter(string characterId, string characterName, IEnumerable<string> otherIds)
        {
            PlayerPrefs.SetString(CharacterKey, characterId);
            PlayerPrefs.SetString(NameKey, characterName);
            PlayerPrefs.SetString(OthersKey, string.Join(",", otherIds));
            PlayerPrefs.Save();
        }

        /// <summary>Remembers the question set the quiz sample plays.</summary>
        /// <param name="questionSetId">The picked question set.</param>
        public static void SaveQuestionSet(string questionSetId)
        {
            PlayerPrefs.SetString(QuestionSetKey, questionSetId);
            PlayerPrefs.Save();
        }

        /// <summary>
        ///     Gives the saved picks to the agents in <paramref name="scene"/>, after their Awake and
        ///     before their Start. A group sends each character once, so in a scene with a
        ///     <see cref="NeocortexGroupDirector"/> the first cast member gets the picked character and
        ///     the rest get your other characters in turn. Quiz agents get the picked character and
        ///     question set.
        /// </summary>
        /// <param name="scene">The sample scene that just loaded.</param>
        public static void ApplyPicks(Scene scene)
        {
            string picked = SavedCharacterId;
            if (string.IsNullOrEmpty(picked)) return;
            string[] others = PlayerPrefs.GetString(OthersKey, "").Split(',').Where(id => id != "").ToArray();
            GameObject[] roots = scene.GetRootGameObjects();

            string questionSet = SavedQuestionSetId;
            foreach (NeocortexQuizAgent quiz in roots.SelectMany(root => root.GetComponentsInChildren<NeocortexQuizAgent>(true)))
            {
                quiz.characterID = picked;
                if (questionSet != "") quiz.questionSetID = questionSet;
            }
            string hostName = PlayerPrefs.GetString(NameKey, "");
            foreach (QuizShowSample show in roots.SelectMany(root => root.GetComponentsInChildren<QuizShowSample>(true)))
            {
                if (hostName != "") show.hostName = hostName;
            }

            List<NeocortexSmartAgent> agents = roots.SelectMany(root => root.GetComponentsInChildren<NeocortexSmartAgent>(true)).ToList();
            NeocortexGroupDirector director = roots
                .Select(root => root.GetComponentInChildren<NeocortexGroupDirector>(true)).FirstOrDefault(d => d != null);

            if (director == null)
            {
                foreach (NeocortexSmartAgent agent in agents) agent.characterID = picked;
                return;
            }

            List<NeocortexSmartAgent> cast = director.Agents.Where(a => a != null).ToList();
            cast.AddRange(agents.Except(cast));
            for (int i = 0; i < cast.Count; i++)
            {
                cast[i].characterID = i == 0 || others.Length == 0 ? picked : others[(i - 1) % others.Length];
            }
        }

        /// <summary>Opens a scene by path: from Build Settings when it is listed, else straight from the project in the editor.</summary>
        /// <param name="path">The scene's asset path.</param>
        /// <returns>False when the scene can't be opened (a build without it in Build Settings).</returns>
        public static bool OpenScene(string path)
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(path);
            if (buildIndex >= 0)
            {
                SceneManager.LoadScene(buildIndex);
                return true;
            }
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            return true;
#else
            return false;
#endif
        }

        /// <summary>Shows the Back button, keeps it across the scene change and opens the sample.</summary>
        /// <param name="launcherScenePath">The launcher scene's asset path, to come back to.</param>
        /// <param name="samplePath">The sample scene's asset path.</param>
        public void Open(string launcherScenePath, string samplePath)
        {
            launcherPath = launcherScenePath;
            gameObject.SetActive(true);
            DontDestroyOnLoad(gameObject);
            OpenScene(samplePath);
        }

        private void Awake()
        {
            backButton.onClick.AddListener(Back);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            bool back = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            bool back = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (back) Back();
        }

        // The launcher scene brings its own inactive copy, so this one goes.
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path == launcherPath)
            {
                Destroy(gameObject);
                return;
            }
            ApplyPicks(scene);
        }

        private void Back()
        {
            OpenScene(launcherPath);
        }
    }
}
