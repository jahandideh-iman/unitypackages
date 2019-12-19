

using System;
using System.Collections.Generic;

namespace Arman.Foundation.Core.ConfigurationManagement
{
    public class DynamicConfigurer<T> : Configurer<T>
    {
        private List<Action<T>> configActions = new List<Action<T>>();


        public void AddConfigAction(Action<T> action)
        {
            configActions.Add(action);
        }

        public void Configure(T entity)
        {
            foreach (var confAction in configActions)
                TryExecute(confAction, entity);
                
        }

        private void TryExecute(Action<T> confAction, T entity)
        {
            try
            {
                confAction(entity);
            }
            catch(Exception e)
            {
                UnityEngine.Debug.LogErrorFormat(
                    "Error executing config command {0} on {1} \n Reason: {2}", 
                    confAction, 
                    entity,
                    e);
            }
        }

        public void RegisterSelf(ConfigurationManager manager)
        {
            manager.Register(this);
        }
    }
}