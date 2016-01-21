using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace AnimeAssessor
{
    public class AnimeTvRepository
    {
        public List<string> GetTvTitles(string filePath)
        {
            return LoadXML(filePath, "item", "name").OrderBy(x=> x).ToList();
        }

        private static IEnumerable<string> LoadXML(string filePath, string descendant, string element)
        {
            return XElement.Load(filePath)
                    .Elements(descendant)
                    .Where(c => c.Element("type").Value == "TV")
                    .Select(c => DeAccentTitles(c.Element(element).Value));
        }

        private static string DeAccentTitles(string title)
        {
            char[] chars = title.Normalize(NormalizationForm.FormD)
                 .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                 .ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }
    }
}
