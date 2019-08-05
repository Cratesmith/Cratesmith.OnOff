using System.Collections;
using Cratesmith.Utils;
using UnityEngine;
#if UNITY_EDITOR

#endif

namespace Cratesmith.OnOff
{
    [RequireComponent(typeof(CanvasGroup))]
    public class OnOffTransitionUIFade : OnOffTransition
    {
        [SerializeField] private float m_switchOnDelay = 0.0f;
        [SerializeField] private float m_switchOffDelay = 0.0f;
        [SerializeField] private float m_switchOnDuration = 0.125f;
        [SerializeField] private float m_switchOffDuration = 0.125f;
        [SerializeField] private float m_onAlpha = 1f;
        [SerializeField] private float m_offAlpha = 0f;
        [SerializeField] private bool m_useRealTime = true;
        private CanvasGroup m_canvasGroup;
        private Coroutine m_coroutine;

        void Awake()
        {
            m_canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
            m_canvasGroup.alpha = onOff.state==OnOff.Status.On ? m_onAlpha : m_offAlpha;
        }

        protected override void OnSwitchingOn()
        {
            if (m_coroutine!=null)
            {
                StopCoroutine(m_coroutine);
            }

            if (isActiveAndEnabled)
            {
                m_coroutine = StartCoroutine(TransitionCoroutine(m_canvasGroup.alpha, m_onAlpha, m_switchOnDelay, m_switchOnDuration));
            }
            else
            {
                Finish();
            }
        }

        protected override void OnSwitchedOn()
        {
            m_canvasGroup.alpha = m_onAlpha;	
        }

        protected override void OnSwitchingOff()
        {
            if (m_coroutine!=null)
            {
                StopCoroutine(m_coroutine);
            }

            if (isActiveAndEnabled)
            {
                m_coroutine = StartCoroutine(TransitionCoroutine(m_canvasGroup.alpha, m_offAlpha, m_switchOffDelay,
                    m_switchOffDuration));
            }
            else
            {
                Finish();
            }
        }

        protected override void OnSwitchedOff()
        {
            m_canvasGroup.alpha = m_offAlpha;	
        }

        public float currentTime { get { return m_useRealTime ? Time.realtimeSinceStartup : Time.time; }}

        private IEnumerator TransitionCoroutine(float fromAlpha, float toAlpha, float delay, float duration)
        {
            if (delay > 0)
            {
                if (m_useRealTime) yield return new WaitForSecondsRealtime(delay);
                else yield return new WaitForSeconds(delay);
            }

            var realDuration = Mathf.Abs(toAlpha - fromAlpha) * duration;
            var startTime = currentTime;
            var time = 0f;
            while (time < realDuration)
            {
                yield return 0;
                var t = time / realDuration;			
                m_canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                time = currentTime - startTime;
            } 
            m_canvasGroup.alpha = toAlpha;	
            Finish();
        }
    }
}
