using EFT;

namespace MiyakoCarryService.Client.Datas
{
    public class FormationData : BaseData
    {
        public MongoID Id;
        public string Name;
        public string FormationMatrix;

        public FormationData(string name, string formationMatrix) : base()
        {
            Id = MongoID.Generate();
            Name = name;
            FormationMatrix = formationMatrix;
        }

        public FormationData(MongoID id, string name, string formationMatrix) : base()
        {
            Id = id;
            Name = name;
            FormationMatrix = formationMatrix;
        }
    }
}