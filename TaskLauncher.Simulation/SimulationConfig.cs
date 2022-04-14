namespace TaskLauncher.Simulation;

/// <summary>
/// Trida pro konfiguraci simulace
/// </summary>
public class SimulationConfig
{
    public int NormalUsers { get; set; }
    public int VipUsers { get; set; }
    public int TaskCount { get; set; }
    public int DelayMin { get; set; }
    public int DelayMax { get; set; }
}
