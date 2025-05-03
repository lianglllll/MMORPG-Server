using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Event
{
    public class CharacterEventSystem
    {
        private Dictionary<string, Action<Dictionary<string, object>>> m_eventListeners = new();
        public void Subscribe(string eventType, Action<Dictionary<string, object>> handler)
        {
            if (!m_eventListeners.ContainsKey(eventType))
            {
                m_eventListeners[eventType] = handler;
            }
            else
            {
                m_eventListeners[eventType] += handler;
            }
        }
        public void Unsubscribe(string eventType, Action<Dictionary<string, object>> handler)
        {
            if (m_eventListeners.TryGetValue(eventType, out var currentHandler))
            {
                // 从委托链中移除指定handler
                currentHandler -= handler;

                // 更新委托链
                if (currentHandler != null)
                    m_eventListeners[eventType] = currentHandler; // 更新为移除后的委托
                else
                    m_eventListeners.Remove(eventType); // 无剩余handler则移除事件类型
            }
        }
        public void Trigger(string eventType, Dictionary<string, object> parameters = null)
        {
            if (m_eventListeners.ContainsKey(eventType))
            {
                m_eventListeners[eventType]?.Invoke(parameters ?? new Dictionary<string, object>());
            }
        }
    }
}
