using System;

namespace Arman.UpdateManagement.Tests
{
    public class UpdatableMock : IUpdatable
    {
        public Action<float> onUpdateAction = delegate { };

        int updateCalls = 0;

        public int UpdateCallCount()
        {
            return updateCalls;
        }

        public bool IsUpdated()
        {
            return updateCalls > 0;
        }

        public void UpdateTime(float amount)
        {
            updateCalls++;
            onUpdateAction(amount);
        }
    }
}
