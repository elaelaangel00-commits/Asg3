namespace Backrooms.Puzzles.Simon
{
    // every simon state must have these 5 functions.
    // that's why SimonPuzzle can switch between states without knowing which one it has.
    public interface ISimonState
    {
        void Enter();                         // runs once when we enter the state (setup)
        void Tick(float deltaTime);           // runs every frame
        void OnStationShot(int stationId);    // a target was shot
        void OnStartShot();                   // the START button was shot
        void Exit();                          // runs once when we leave the state (cleanup)
    }
}
