using System.Collections;
using Cratesmith.Utils;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
#endif

namespace Cratesmith.OnOff
{
    [RequireComponent(typeof(Graphic))]
    public class OnOffTransitionUIColor : OnOffTransition
    {
        [SerializeField] private float m_switchOnDelay = 0.0f;
        [SerializeField] private float m_switchOffDelay = 0.0f;
        [SerializeField] private float m_switchOnDuration = 0.125f;
        [SerializeField] private float m_switchOffDuration = 0.125f;
        [SerializeField] private Color m_onColor = Color.white;
        [SerializeField] private Color m_offColor = new Color(1,1,1,0);
        [SerializeField] private bool m_useRealTime = true;
        private Graphic m_graphic;
        private Coroutine m_coroutine;


        void Awake()
        {
            m_graphic = gameObject.GetOrAddComponent<Graphic, Image>();
            m_graphic.color = onOff.state==OnOff.Status.On ? m_onColor : m_offColor;
        }

        protected override void OnSwitchingOn()
        {
            if (m_coroutine!=null)
            {
                StopCoroutine(m_coroutine);
            }
            m_coroutine = StartCoroutine(TransitionCoroutine(m_graphic.color, m_onColor, m_switchOnDelay, m_switchOnDuration));
        }

        protected override void OnSwitchedOn()
        {
            m_graphic.color = m_onColor;	
        }

        protected override void OnSwitchingOff()
        {
            if (m_coroutine!=null)
            {
                StopCoroutine(m_coroutine);
            }
            m_coroutine = StartCoroutine(TransitionCoroutine(m_graphic.color, m_offColor, m_switchOffDelay, m_switchOffDuration));		
        }

        protected override void OnSwitchedOff()
        {
            m_graphic.color = m_offColor;	
        }

        public float currentTime { get { return m_useRealTime ? Time.realtimeSinceStartup : Time.time; }}

        private IEnumerator TransitionCoroutine(Color fromColor, Color toColor, float delay, float duration)
        {
            if (delay > 0)
            {
                if (m_useRealTime) yield return new WaitForSecondsRealtime(delay);
                else yield return new WaitForSeconds(delay);
            }

		
            var realDuration = Mathf.Max(Mathf.Abs(toColor.r - fromColor.r),Mathf.Abs(toColor.g - fromColor.g),Mathf.Abs(toColor.b - fromColor.b)) * duration;
            var startTime = currentTime;
            var time = 0f;
            while (time < realDuration)
            {
                yield return 0;
                var t = time / realDuration;			
                m_graphic.color = Color.Lerp(fromColor, toColor, t);
                time = currentTime - startTime;
            } 
            m_graphic.color = toColor;	
            Finish();
        }
    }
}