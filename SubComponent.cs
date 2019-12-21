using System.Collections.Generic;
using UnityEngine;

namespace Cratesmith.OnOff
{
    public abstract class SubComponent<T> : MonoBehaviour
    {
        private T m_owner;
        public T owner 
        {
            get 
            {
                if(EqualityComparer<T>.Default.Equals(m_owner, default(T)))
                {
                    m_owner = GetComponentInParent<T>();
                }
                return m_owner;
            }
        }
    }
}