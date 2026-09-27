using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Content
{
    /// <summary>Loads read-only design content (jobs, products) from Resources/Content once at startup. Mirrors the Resources.Load + JsonUtility pattern already used by Millionaire/QuestionLoader.</summary>
    public class ContentDatabase
    {
        private const string JobsResourcePath = "Content/jobs";
        private const string ProductsResourcePath = "Content/products";
        private const string TutorialResourcePath = "Content/tutorial";
        private const string DreamsResourcePath = "Content/dreams";
        private const string FinanceResourcePath = "Content/finance";
        private const string StudyThemesResourcePath = "Study/themes";
        private const string StudyTasksResourceFolder = "Study/Tasks";

        public IReadOnlyList<JobDefinition> Jobs { get; }
        public IReadOnlyList<ProductDefinition> Products { get; }
        public IReadOnlyList<TutorialStepDefinition> TutorialSteps { get; }

        /// <summary>In dreams.json order — the order they're shown and auto-picked in.</summary>
        public IReadOnlyList<DreamDefinition> Dreams { get; }

        public FinanceSettings Finance { get; }

        /// <summary>[0] = level 1.</summary>
        public IReadOnlyList<string> CareerTitles { get; }

        /// <summary>Sorted by order.</summary>
        public IReadOnlyList<StudyThemeDefinition> StudyThemes { get; }

        /// <summary>Every task file, sorted by theme order, then task order.</summary>
        public IReadOnlyList<StudyTaskDefinition> StudyTasks { get; }

        public ContentDatabase()
        {
            Jobs = LoadJobs();
            Products = LoadProducts();
            TutorialSteps = LoadTutorialSteps();
            Dreams = LoadDreams();
            Finance = LoadFinance();

            StudyThemesFile studyFile = LoadStudyThemes();
            CareerTitles = studyFile.careerTitles?.ToList() ?? new List<string>();
            StudyThemes = (studyFile.themes ?? new StudyThemeDefinition[0]).OrderBy(t => t.order).ToList();
            StudyTasks = LoadStudyTasks(StudyThemes);
        }

        public IEnumerable<StudyTaskDefinition> StudyTasksOf(string themeId) => StudyTasks.Where(t => t.theme == themeId);
        public StudyTaskDefinition FindStudyTask(string id) => StudyTasks.FirstOrDefault(t => t.id == id);

        public JobDefinition FindJob(string id) => Jobs.FirstOrDefault(j => j.id == id);
        public ProductDefinition FindProduct(string id) => Products.FirstOrDefault(p => p.id == id);
        public DreamDefinition FindDream(string id) => Dreams.FirstOrDefault(d => d.id == id);

        /// <summary>Jobs shown on the Work screen for this level. The list is level-specific, not cumulative — see PLAN.md section 3.</summary>
        public IEnumerable<JobDefinition> JobsForLevel(int level) => Jobs.Where(j => j.level == level);

        private static List<JobDefinition> LoadJobs()
        {
            TextAsset asset = Resources.Load<TextAsset>(JobsResourcePath);
            if (asset == null)
            {
                Debug.LogError($"ContentDatabase: Resources/{JobsResourcePath}.json not found.");
                return new List<JobDefinition>();
            }

            JobDefinitionList wrapper = JsonUtility.FromJson<JobDefinitionList>(asset.text);
            return wrapper?.jobs?.ToList() ?? new List<JobDefinition>();
        }

        private static List<ProductDefinition> LoadProducts()
        {
            TextAsset asset = Resources.Load<TextAsset>(ProductsResourcePath);
            if (asset == null)
            {
                Debug.LogError($"ContentDatabase: Resources/{ProductsResourcePath}.json not found.");
                return new List<ProductDefinition>();
            }

            ProductDefinitionList wrapper = JsonUtility.FromJson<ProductDefinitionList>(asset.text);
            return wrapper?.products?.ToList() ?? new List<ProductDefinition>();
        }

        private static List<TutorialStepDefinition> LoadTutorialSteps()
        {
            TextAsset asset = Resources.Load<TextAsset>(TutorialResourcePath);
            if (asset == null)
            {
                Debug.LogError($"ContentDatabase: Resources/{TutorialResourcePath}.json not found.");
                return new List<TutorialStepDefinition>();
            }

            TutorialStepDefinitionList wrapper = JsonUtility.FromJson<TutorialStepDefinitionList>(asset.text);
            return wrapper?.steps?.ToList() ?? new List<TutorialStepDefinition>();
        }

        private static List<DreamDefinition> LoadDreams()
        {
            TextAsset asset = Resources.Load<TextAsset>(DreamsResourcePath);
            if (asset == null)
            {
                Debug.LogError($"ContentDatabase: Resources/{DreamsResourcePath}.json not found.");
                return new List<DreamDefinition>();
            }

            DreamDefinitionList wrapper = JsonUtility.FromJson<DreamDefinitionList>(asset.text);
            return wrapper?.dreams?.ToList() ?? new List<DreamDefinition>();
        }

        /// <summary>Missing file = the defaults from FinanceSettings' field initializers.</summary>
        private static FinanceSettings LoadFinance()
        {
            TextAsset asset = Resources.Load<TextAsset>(FinanceResourcePath);
            if (asset == null)
            {
                Debug.LogError($"ContentDatabase: Resources/{FinanceResourcePath}.json not found, using defaults.");
                return new FinanceSettings();
            }

            return JsonUtility.FromJson<FinanceSettings>(asset.text) ?? new FinanceSettings();
        }

        private static StudyThemesFile LoadStudyThemes()
        {
            TextAsset asset = Resources.Load<TextAsset>(StudyThemesResourcePath);
            if (asset == null)
            {
                Debug.LogError($"ContentDatabase: Resources/{StudyThemesResourcePath}.json not found.");
                return new StudyThemesFile();
            }

            return JsonUtility.FromJson<StudyThemesFile>(asset.text) ?? new StudyThemesFile();
        }

        /// <summary>Every json in Resources/Study/Tasks, one task per file. Broken/unknown ones are skipped with a warning naming the file.</summary>
        private static List<StudyTaskDefinition> LoadStudyTasks(IReadOnlyList<StudyThemeDefinition> themes)
        {
            var tasks = new List<StudyTaskDefinition>();

            foreach (TextAsset asset in Resources.LoadAll<TextAsset>(StudyTasksResourceFolder))
            {
                StudyTaskDefinition task;
                try
                {
                    task = JsonUtility.FromJson<StudyTaskDefinition>(asset.text);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"ContentDatabase: study task '{asset.name}.json' is not valid json: {e.Message}");
                    continue;
                }

                if (task == null || string.IsNullOrEmpty(task.id))
                {
                    Debug.LogWarning($"ContentDatabase: study task '{asset.name}.json' has no id, skipped.");
                    continue;
                }

                if (!task.IsChoose && !task.IsDistribute)
                {
                    Debug.LogWarning($"ContentDatabase: study task '{task.id}' has unknown type '{task.type}', skipped.");
                    continue;
                }

                if (task.IsChoose && (task.options == null || task.options.Length == 0))
                {
                    Debug.LogWarning($"ContentDatabase: choose task '{task.id}' has no options, skipped.");
                    continue;
                }

                if (task.IsDistribute && (task.rows == null || task.rows.Length == 0))
                {
                    Debug.LogWarning($"ContentDatabase: distribute task '{task.id}' has no rows, skipped.");
                    continue;
                }

                if (themes.All(t => t.id != task.theme))
                {
                    Debug.LogWarning($"ContentDatabase: study task '{task.id}' points to unknown theme '{task.theme}', skipped.");
                    continue;
                }

                if (tasks.Any(t => t.id == task.id))
                {
                    Debug.LogWarning($"ContentDatabase: duplicate study task id '{task.id}' ('{asset.name}.json'), skipped.");
                    continue;
                }

                tasks.Add(task);
            }

            return tasks
                .OrderBy(t => themes.First(th => th.id == t.theme).order)
                .ThenBy(t => t.order)
                .ToList();
        }
    }
}
