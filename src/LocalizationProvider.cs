using System.Collections.Generic;
using System.IO;

namespace SimplePlanes2PartEditor
{
    internal sealed class LocalizationProvider
    {
        private readonly string _localizationDirectory;
        private Dictionary<string, string> _texts = new Dictionary<string, string>();

        public LocalizationProvider(string localizationDirectory)
        {
            _localizationDirectory = localizationDirectory;
        }

        public string Language { get; private set; }

        public void Load(string language)
        {
            string requestedLanguagePath = Path.Combine(_localizationDirectory, language + ".json");
            string fallbackLanguagePath = Path.Combine(_localizationDirectory, "en-US.json");
            string path = File.Exists(requestedLanguagePath) ? requestedLanguagePath : fallbackLanguagePath;

            Language = File.Exists(path) ? Path.GetFileNameWithoutExtension(path) : language;
            _texts = File.Exists(path)
                ? SimpleJson.ReadFlatObject(File.ReadAllText(path, System.Text.Encoding.UTF8))
                : new Dictionary<string, string>();
        }

        public string Get(string key)
        {
            string value;
            if (_texts.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }
    }
}
