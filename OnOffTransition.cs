using Cratesmith;
using Cratesmith.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace Cratesmith.OnOff
{
    [RequireComponent(typeof(OnOff))]
    public class OnOffTransition<T> : SubComponent<T>, IOnOffTransition
    {
        protected virtual bool isOnTransition { get { return true; }}
        protected virtual bool isOffTransition { get { return true; }}

        protected virtual void OnEnable()
        {
            if(isOnTransition) onOff.switchingOn.onBeginTransition.AddListener(EnterSwitchingOn);
            if(isOffTransition) onOff.switchingOff.onBeginTransition.AddListener(EnterSwitchingOff);
        }

        protected virtual void OnDisable()
        {
            if(ApplicationState.isQuitting) return;
            onOff.switchingOn.onBeginTransition.RemoveListener(EnterSwitchingOn);
            onOff.switchingOff.onBeginTransition.RemoveListener(EnterSwitchingOff);
        }

        private void EnterSwitchingOn()
        {
            Assert.AreEqual(OnOff.Status.SwitchingOn, onOff.state);
            onOff.switchingOn.AddTransition(this);
            OnSwitchingOn();
        }

        private void EnterSwitchingOff()
        {
            Assert.AreEqual(OnOff.Status.SwitchingOff, onOff.state);
            onOff.switchingOff.AddTransition(this);
            OnSwitchingOff();
        }

        protected void Finish()
        {
            switch (onOff.state)
            {
                case OnOff.Status.SwitchingOn: 
                    onOff.switchingOn.RemoveTransition(this);
                    break;

                case OnOff.Status.SwitchingOff: 
                    onOff.switchingOff.RemoveTransition(this);
                    break;
            }
        }

        protected virtual void OnSwitchingOff() {}
        protected virtual void OnSwitchingOn()	{}

        private OnOff m_onOff;
        public OnOff onOff 
        {
            get 
            {
                if(!isOnOffCached)
                {
                    m_onOff = FindOnOff();
                    Assert.IsTrue(isOnOffCached, string.Format("OnOffTransition {0} does not have an onoff!", name));
                }
                return m_onOff;
            }
        }

        protected virtual OnOff FindOnOff()
        {
            return GetComponent<OnOff>(); 
        }

        private bool isOnOffCached { get { return m_onOff!=null; }}
    }

    public interface IOnOffTransition { }

    [RequireComponent(typeof(OnOff))]
    public class OnOffTransition : MonoBehaviour, IOnOffTransition
    {
        protected virtual bool isOnTransition { get { return true; }}
        protected virtual bool isOffTransition { get { return true; }}

        protected virtual void OnEnable()
        {
            if (isOnTransition)
            {
                onOff.switchingOn.onBeginTransition.AddListener(EnterSwitchingOn);
                onOff.on.onEnter.AddListener(EnterSwitchedOn);
            }
            if (isOffTransition)
            {
                onOff.switchingOff.onBeginTransition.AddListener(EnterSwitchingOff);
                onOff.off.onEnter.AddListener(EnterSwitchedOff);
            }
        }

        protected virtual void OnDisable()
        {
            if(ApplicationState.isQuitting) return;
            onOff.off.onEnter.RemoveListener(EnterSwitchedOff);
            onOff.on.onEnter.RemoveListener(EnterSwitchedOn);
            onOff.switchingOn.onBeginTransition.RemoveListener(EnterSwitchingOn);
            onOff.switchingOff.onBeginTransition.RemoveListener(EnterSwitchingOff);
        }

        private void EnterSwitchingOn()
        {
            if(OnOff.Status.SwitchingOn != onOff.state) Debug.Log($"Expected {OnOff.Status.SwitchingOn}, got {onOff.state}");
            //Assert.AreEqual(OnOff.Status.SwitchingOn, onOff.state);
            onOff.switchingOn.AddTransition(this);
            OnSwitchingOn();
        }

        private void EnterSwitchedOn()
        {
            OnSwitchedOn();
        }

        private void EnterSwitchedOff()
        {
            OnSwitchedOff();
        }

        private void EnterSwitchingOff()
        {
            if(OnOff.Status.SwitchingOff != onOff.state) Debug.Log($"Expected {OnOff.Status.SwitchingOff}, got {onOff.state}");
            //Assert.AreEqual(OnOff.Status.SwitchingOff, onOff.state);
            onOff.switchingOff.AddTransition(this);
            OnSwitchingOff();
        }

        protected void Finish()
        {
            switch (onOff.state)
            {
                case OnOff.Status.SwitchingOn: 
                    onOff.switchingOn.RemoveTransition(this);
                    break;

                case OnOff.Status.SwitchingOff: 
                    onOff.switchingOff.RemoveTransition(this);
                    break;
            }
        }

        protected virtual void OnSwitchingOff() {}
        protected virtual void OnSwitchedOff() {}
        protected virtual void OnSwitchingOn()	{}
        protected virtual void OnSwitchedOn()	{}

        private OnOff m_onOff;
        public OnOff onOff 
        {
            get 
            {
                if(!isOnOffCached)
                {
                    m_onOff = FindOnOff();
                    Assert.IsTrue(isOnOffCached, string.Format("OnOffTransition {0} does not have an onoff!", name));
                }
                return m_onOff;
            }
        }

        protected virtual OnOff FindOnOff()
        {
            return GetComponent<OnOff>(); 
        }

        private bool isOnOffCached { get { return m_onOff!=null; }}
    }
}