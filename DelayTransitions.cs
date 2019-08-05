using System.Collections;
using Cratesmith.Utils;
using UnityEngine;

namespace Cratesmith.OnOff
{
    public class DelayTransitions : SubComponent<OnOff>
    {
        private bool m_delaying;

        void OnEnable()
        {
            owner.switchingOn.onEnter.AddListener(OnSwitching);
            owner.switchingOff.onEnter.AddListener(OnSwitching);
        }

        void OnDisable()
        {
            owner.switchingOn.onEnter.RemoveListener(OnSwitching);
            owner.switchingOff.onEnter.RemoveListener(OnSwitching);
            
        }

        private void OnSwitching()
        {
            StartCoroutine(DelayCoroutine());
        }

        public IEnumerator DelayCoroutine()
        {
            yield return new WaitForSeconds(3f);
            owner.FinishTransition();
        }
    }
}