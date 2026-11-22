using EFT;

using MiyakoCarryService.Client.Extensions;
namespace MiyakoCarryService.Client.Datas
{
    public class FormationData : BaseData
    {
        public string Id;
        public string Name;
        public string FormationMatrix;

        public FormationData(string name, string formationMatrix) : base()
        {
            Id = MongoID.Generate().MongoIdText();
            Name = name;
            FormationMatrix = formationMatrix;
        }

        public FormationData(string id, string name, string formationMatrix) : base()
        {
            Id = id;
            Name = name;
            FormationMatrix = formationMatrix;
        }
    }
}