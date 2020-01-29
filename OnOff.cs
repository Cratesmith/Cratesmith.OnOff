using System;
using System.Collections.Generic;
using Cratesmith.Collections.Temp;
using Cratesmith.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
#if UNITY_EDITOR

#endif

namespace Cratesmith.OnOff
{   
    public class OnOff : MonoBehaviour
    {
        public enum Status
        {
            On=0,    
            Off,            
            SwitchingOn,
            SwitchingOff
        }
        private StateMachine m_statemachine;
        
     
        [SerializeField] private StateOff m_offState = new StateOff();
        public StateOff off                     { get { return m_offState; }}
        
        [SerializeField] private StateOn    m_onState = new StateOn();
        public StateOn  on                      { get { return m_onState; }}
       
        [SerializeField] private StateSwitchingOn    m_switchingOnState = new StateSwitchingOn();
        public StateSwitchingOn switchingOn     { get { return m_switchingOnState; }}
               
        [SerializeField] private StateSwitchingOff m_switchingOffState = new StateSwitchingOff();
        public StateSwitchingOff switchingOff   { get { return m_switchingOffState; }}

        [SerializeField] private bool       m_debug = false;
        [SerializeField] private bool       m_isOnSelf  = true;

        [FormerlySerializedAs("m_onState.enableGameObject")]
        public bool onEnablesGameObject = true;

        [FormerlySerializedAs("m_offState.disableGameObject")]
        public bool offDisablesGameObject = true;

        public UnityEvent onChanged;
        Action<OnOff> m_onStateChangeComplete;

        public bool     isOnSelf                { get { return m_isOnSelf; } }
        public bool     isOn                    { get { Init(); return m_statemachine.currentState.isOn; } }
        public Status   state                   { get { Init(); return m_statemachine.currentStateId; }}
        public bool     isDebug                 { get { return m_debug; } }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {            
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

	        EditorApplication.delayCall += () =>
	        {
		        if (!this) return;
		        var parent = GetParent();
		        if (parent != null)
		        {
			        parent.OnValidate();
		        }

		        Init();
		        Apply(true);
	        };
        }
#endif

        void OnEnable()
        {
            Init();
        }

        void OnDisable()
        {
            if (ApplicationState.isQuitting) return;
            FinishTransition(true);
        }

        public void Switch(bool newState)
        {
            Switch(newState, false);
        }

        public void Switch(bool newState, Action<OnOff> onComplete)
        {
            Switch(newState, false, onComplete);
        }

        public void Switch(bool newState, bool immediate, Action<OnOff> onComplete=null)
        {
            Init();            

            m_isOnSelf = newState;
	        var parentStateMachine = GetParent<OnOffStateMachine>();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var so = new SerializedObject(this);
                var prop = so.FindProperty("m_isOnSelf");
                prop.boolValue = newState;
                so.ApplyModifiedProperties();              
            }
#endif

            if (newState && parentStateMachine != null && parentStateMachine.currentState != this)
            {
                parentStateMachine.SetState(this, immediate);
	            return;
            }

	        Apply(immediate, onComplete);
        }

        protected void DoSwitch(bool newState, bool immediate) {}

        private void Apply(bool immediate, Action<OnOff> onComplete=null)
        {
            Init();
            if (m_statemachine.currentState == null) return;

            var prevState = m_statemachine.currentStateId;
            m_statemachine.currentState.Switch(GetHeirachyState(), immediate);

            if (prevState == m_statemachine.currentStateId)
            {
                onComplete?.Invoke(this);
                return;
            };

            if (onComplete!=null)
            {
                m_onStateChangeComplete += onComplete;
            }
            OnStateChanged(immediate);
        }

        private void OnStateChanged(bool immediate)
        {
            var parent = GetParent();
            if (parent != null)
            {
                parent.OnChildChangedState(this, immediate);
            }

			using(var tempList = GetChildrenTempList())
            foreach (var onOff in tempList)
            {
                onOff.Apply(immediate);
            }

            var transition = m_statemachine.currentState as StateTransition;
            if (transition != null)
            {
                transition.AttemptToSwitch(false);
            }
            else
            {
                m_onStateChangeComplete?.Invoke(this);
                m_onStateChangeComplete = null;
            }
        }

        protected virtual void OnChildChangedState(OnOff onOff, bool immediate)
        {
            Init();
            if (m_statemachine.currentState == null) return;
            m_statemachine.currentState.OnChildStateChanged(onOff);
        }

        private T GetParent<T>() where T:OnOff
        {
            return transform.parent!=null ? transform.parent.GetComponent<T>():null;
        }

        private OnOff GetParent()   
        {
            return transform.parent!=null ? transform.parent.GetComponent<OnOff>():null;
        }

        private void Init()
        {
            if (m_statemachine != null) return;
            m_statemachine = new StateMachine();
            m_statemachine.Init(this);
            m_statemachine.AddState(Status.Off, m_offState);
            m_statemachine.AddState(Status.On, m_onState);
            m_statemachine.AddState(Status.SwitchingOn, m_switchingOnState);
            m_statemachine.AddState(Status.SwitchingOff, m_switchingOffState);
            m_statemachine.SetState(GetHeirachyState() ? Status.On : Status.Off);            
            m_statemachine.onStateChanged += OnChange;

            using(var children = GetChildrenTempList())
            foreach (var child in children)
            {
                child.Init();
            }
        }

        private void OnChange(State _arg1, State _arg2)
        {
            if(onChanged!=null) onChanged.Invoke();
        }

        private bool GetHeirachyState()
        {
            var onOff = this;
            while (onOff != null)
            {
                if (!onOff.m_isOnSelf)
                {
                    return false;
                }

                onOff = onOff.GetParent();
            }
            return true;
        }

        private TempList<OnOff> GetChildrenTempList()
        {
            var list = TempList<OnOff>.Get();
            for (int i = 0; i < transform.childCount; i++)
            {
                var onOff = transform.GetChild(i).GetComponent<OnOff>();
                if (onOff == null) continue;
                list.Add(onOff);
            }
            return list;
        }

        public void FinishTransition(bool force=false)
        {
            Init();
            var transition = m_statemachine.currentState as StateTransition;
            if (transition!=null)
            {
                transition.Finish(force);
            }
        }

        #region states
        public class StateMachine : StateMachineWithId<State, StateMachine, Status>
        {
            public void Init(OnOff onOff)
            {
                owner = onOff;
                defaultState = owner.@on;
            }

            public OnOff owner { get; private set; }
        }

        [Serializable]
        public abstract class State : State<State, OnOff.StateMachine>
        {
	        public UnityEvent onEnter = new UnityEvent();
            public UnityEvent onExit = new UnityEvent();
            public OnOff owner { get { return stateMachine.owner; } }
            public abstract bool isOn { get; }

            public void Switch(bool on, bool immediate)
            {
                if (immediate)
                {
                    stateMachine.SetState(on ? OnOff.Status.On : OnOff.Status.Off);
                }
                else
                {
                    DoSwitch(on);
                }
            }

            public override void OnEnter()
            {
                if (owner.m_debug)
                {
                    Debug.LogFormat(owner,"[{0},{1},{2:.00}]: OnEnter", owner.name, GetType().Name, Time.realtimeSinceStartup);
                }

				#if UNITY_EDITOR
	            if (!EditorApplication.isPlayingOrWillChangePlaymode) return;
				#endif
                if (onEnter != null) onEnter.Invoke();
            }

            public override void OnExit()
            {
                if (owner.m_debug)
                {
                    Debug.LogFormat(owner,"[{0},{1},{2:.00}]: OnExit", owner.name, GetType().Name, Time.realtimeSinceStartup);
                }

#if UNITY_EDITOR
	            if (!EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif
	            if (onExit != null) onExit.Invoke();
            }

            public virtual void OnChildStateChanged(OnOff onOff) {}
            protected abstract void DoSwitch(bool on);
        }

        [Serializable]  
        public class StateOff : State
        {
            public override bool isOn { get { return false; } }

            protected override void DoSwitch(bool on)
            {
                if (on)
                {
                    stateMachine.SetState(Status.SwitchingOn);
                }
            }

            public override void OnEnter()
            {
                base.OnEnter();
                
                if (owner.offDisablesGameObject)
                {
                    if (owner.m_debug) Debug.LogFormat(owner,"[{0},{1},{2:.00}]: Disabling gameObject", owner.name, GetType().Name, Time.realtimeSinceStartup);
                    owner.gameObject.SetActive(false);
                }
            }
        }

        [Serializable]
        public class StateOn : State
        {
            public override bool isOn { get { return true; } }

            protected override void DoSwitch(bool on)
            {
                if (on) return;
                stateMachine.SetState(Status.SwitchingOff);
            }

            public override void OnEnter()
            {
                if (!owner.gameObject.activeSelf && owner.onEnablesGameObject)
                {
                    if (owner.m_debug) Debug.LogFormat(owner,"[{0},{1},{2:.00}]: Enabling gameObject", owner.name, GetType().Name, Time.realtimeSinceStartup);
                    owner.gameObject.SetActive(true);
                }                
                base.OnEnter();
            }
        }

        public abstract class StateTransition : State
        {
            public UnityEvent onBeginTransition = new UnityEvent();

            public bool waitForChildren = true;
            private bool m_startedTransitions = false;

            protected abstract Status toState { get; }

            HashSet<IOnOffTransition> m_transitions = new HashSet<IOnOffTransition>();

            public override void OnEnter()
            {
                m_startedTransitions = false;
                base.OnEnter();
            }

            public void AddTransition(IOnOffTransition onOffTransition)
            {
                m_transitions.Add(onOffTransition);
            }

            public void RemoveTransition(IOnOffTransition onOffTransition)
            {
                if (m_transitions.Remove(onOffTransition) && m_transitions.Count == 0)
                {
                    AttemptToSwitch(false);
                }                
            }

            public override void OnChildStateChanged(OnOff onOff)
            {
                base.OnChildStateChanged(onOff);
                AttemptToSwitch(false);
            }

            protected bool AttemptToSwitch_WaitForChildren()
            {
                if (waitForChildren)
                {
					using(var children = owner.GetChildrenTempList())
                    foreach (var onOff in children)
                    {
                        if (onOff.state == owner.state)
                        {
                            return false;
                        }
                    }                    
                }
                return true;
            }

            protected bool AttemptToSwitch_WaitForTransitions()
            {
                if (!m_startedTransitions && onBeginTransition!=null)
                {
                    if (owner.m_debug)
                    {
                        Debug.LogFormat(owner, "[{0},{1},{2:.00}]: Starting Transitions", owner.name, GetType().Name, Time.realtimeSinceStartup);
                    }
                    m_startedTransitions = true;
                    onBeginTransition.Invoke();
                }

                if (m_transitions.Count > 0)
                {
                    return false;
                }
                return true;
            }

            protected void AttemptToSwitch_DoSwitch()
            {
                var prevState = stateMachine.currentStateId;
                stateMachine.SetState(toState);
                if (prevState != stateMachine.currentStateId)
                {
                    owner.OnStateChanged(false);
                }
            }

            public void AttemptToSwitch(bool force)
            {
                if (!AttemptToSwitch_WaitForTransitions() && !force) return;
                if (!AttemptToSwitch_WaitForChildren() && !force) return;
                AttemptToSwitch_DoSwitch();
            }

            public void Finish(bool force=false)
            {
                AttemptToSwitch(force);
            }
        }

        [Serializable]
        public class StateSwitchingOn : StateTransition 
        {
            public override bool isOn { get { return true; } }
            protected override Status toState { get { return Status.On; } }

            protected override void DoSwitch(bool on)
            {
                if (on) return;
                stateMachine.SetState(Status.SwitchingOff);
            }

            public override void OnEnter()
            {
                if (!owner.gameObject.activeSelf && owner.onEnablesGameObject)
                {
                    if (owner.m_debug) Debug.LogFormat(owner,"[{0},{1},{2:.00}]: Enabling gameObject", owner.name, GetType().Name, Time.realtimeSinceStartup);
                    owner.gameObject.SetActive(true);
                }   
                base.OnEnter();
            }
        }

        [Serializable]  
        public class StateSwitchingOff : StateTransition 
        {
            public override bool isOn { get { return true; } }
            protected override Status toState { get { return Status.Off; } }
            
            protected override void DoSwitch(bool on)
            {
                if (!on) return;
                stateMachine.SetState(Status.SwitchingOn);
            }
        }        
        #endregion

#if UNITY_EDITOR
        [InitializeOnLoad]
        public class HeirachyWindow
        {
            static HeirachyWindow()
            {
                EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
            }

            public static readonly float BUTTTON_WIDTH = 40;

            private static void HierarchyWindowItemOnGUI(int instanceid, Rect selectionrect)
            {
                DrawElement(EditorUtility.InstanceIDToObject(instanceid), selectionrect);
            }

            static void DrawElement(Object obj, Rect rect)
            {
                if (obj == null) return;
                var textOffset = GUI.skin.GetStyle("label").CalcSize(new GUIContent(obj.name));
                rect = new Rect(rect.x+textOffset.x+20, rect.y, rect.width-textOffset.x, rect.height);
                rect = DrawElement_OnOffButton(obj, rect);
            }

            static Rect DrawElement_OnOffButton(Object obj, Rect rect)
            {
                var go = obj as GameObject;
                if(!go) return rect;
		
                var onOff = go.GetComponent<OnOff>();
                if(!onOff) return rect;
		
                Rect buttonRect = new Rect(rect.x, rect.y, BUTTTON_WIDTH, rect.height);
                var str = "";
                switch(onOff.state)
                {
                    case OnOff.Status.On: str = "On"; break;
                    case OnOff.Status.SwitchingOn: str = "*On"; break;
                    case OnOff.Status.Off: str = "Off"; break;
                    case OnOff.Status.SwitchingOff: str = "*Off"; break;
                }
                var currentVal = onOff.isOnSelf;
                GUI.color = onOff.isOn ? Color.cyan : Color.white;
                var val = GUI.Toggle(buttonRect, currentVal, str, "button");
                GUI.color = Color.white;
                if (currentVal != val)
                {
	                Undo.RegisterFullObjectHierarchyUndo(onOff, "Setting state to "+val);
                    onOff.Switch(val, !Application.isPlaying);  																
	                Undo.FlushUndoRecordObjects();					
                }

                rect.x = buttonRect.xMax;
                return rect;
            }
        }
#endif
    }    
}