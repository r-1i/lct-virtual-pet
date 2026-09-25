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

        public IReadOnlyList<JobDefinition> Jobs { get; }
        public IReadOnlyList<ProductDefinition> Products { get; }
        public IReadOnlyList<TutorialStepDefinition> TutorialSteps { get; }

        public ContentDatabase()
        {
            Jobs = LoadJobs();
            Products = LoadProducts();
            TutorialSteps = LoadTutorialSteps();
        }

        public JobDefinition FindJob(string id) => Jobs.FirstOrDefault(j => j.id == id);
        public ProductDefinition FindProduct(string id) => Products.FirstOrDefault(p => p.id == id);

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
    }
}
