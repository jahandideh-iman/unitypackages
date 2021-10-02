

namespace Arman.UpdateManagement.Foundation
{
    public interface Updatable 
    {
        // NOTE: The name is due to the naming clash with Unity's Update.
        // TODO: Find a better name.
        void UpdateTime(float dt);

        //void OnPaused();
        //void OnResumed();
    }
}