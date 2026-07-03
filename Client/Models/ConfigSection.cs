
namespace MiyakoCarryService.Client.Models
{
    public class ConfigSection
    {
        private int _currentOrder;
        public string Name { get; }

        public ConfigSection(string sectionName, int defaultOrder)
        {
            Name = sectionName;
            _currentOrder = defaultOrder;
        }

        public int GetNextOrder() => _currentOrder--;
    }
}