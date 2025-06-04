using Godot;

namespace IdleAbsurditree.Scripts.Data
{
    [GlobalClass]
    public partial class GameData : Resource
    {
        [Export] public double AvailableNutrients { get; set; } = 0;
        [Export] public double LifetimeNutrients { get; set; } = 0;
        [Export] public double NutrientsPerSecond { get; set; } = 0;
        [Export] public double AutoProductionPerSecond { get; set; } = 0;
        [Export] public double LastSaveTime { get; set; } = 0;
        [Export(PropertyHint.None)]
        public Godot.Collections.Array<int> GeneratorCounts { get; set; } = new();
        
        public void InitializeGeneratorCounts(int count)
        {
            GeneratorCounts.Clear();
            for (int i = 0; i < count; i++)
                GeneratorCounts.Add(0);
        }
    }
}
