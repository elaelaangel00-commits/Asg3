namespace Backrooms.Puzzles.Simon
{
    /// <summary>
    /// One state of the Simon Says puzzle.
    /// Every state answers the same questions: what happens when I start, every frame,
    /// when a target is shot, when START is shot, and when I end.
    /// </summary>
    public interface ISimonState
    {
        void Enter();
        void Tick(float deltaTime);
        void OnStationShot(int stationId);
        void OnStartShot();
        void Exit();
    }
}
