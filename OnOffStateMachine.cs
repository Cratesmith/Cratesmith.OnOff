using System.Linq;
using Cratesmith.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
#if UNITY_EDITOR

#endif

namespace Cratesmith.OnOff
{
    public class OnOffStateMachine : OnOff
    {
        [SerializeField] OnOff m_currentState;
        public OnOff currentState { get { return m_currentState; } }

        public UnityEvent onStateChanged;

#if UNITY_EDITOR
	    protected override void OnValidate()
	    {
		    base.OnValidate();
		    if (EditorApplication.isPlayingOrWillChangePlaymode)
		    {
			    return;
		    }
		    
		    ApplyState(true);
	    }
#endif

        public void SetState(OnOff newState, bool immediate=false)
		{
			if (newState == m_currentState)
			{
				return;
			}

			if (isDebug)
			{
				Debug.LogFormat(this,"[{0},{1},{2:.00}]: SetState {3} {4}", name, GetType().Name, Time.realtimeSinceStartup, newState!=null?newState.name:"Null", immediate?"immediate":"");
			}


			m_currentState = newState;

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				var so = new SerializedObject(this);
				var prop = so.FindProperty("m_currentState");
				prop.objectReferenceValue = newState;
				so.ApplyModifiedProperties();
			}
#endif

			ApplyState(immediate);
			    
		    if (onStateChanged != null)
		    {
                onStateChanged.Invoke();
		    }
		}

		private void ApplyState(bool immediate)
		{
			// do all child states just to be sure 
			using (var states = GetStatesTempList())
				foreach (var childState in states)
				{
					if (childState == m_currentState) continue;
					childState.Switch(false, immediate);
				}

			if (immediate)
			{
				if (m_currentState != null)
				{
					m_currentState.Switch(true, true);
				}
			}

			AttemptToSwitch();
		}

		private void AttemptToSwitch()
        {
            if (m_currentState == null) return;
            if (m_currentState.state != Status.Off) return;

			using(var states = GetStatesTempList())
            foreach (var onOff in states)
            {
                if (onOff != m_currentState && onOff.isOn) return;
            }

            m_currentState.Switch(true, false);
        }

        public TempList<OnOff> GetStatesTempList()
        {
            var states = TempList<OnOff>.Get();
            for (int i = 0; i < transform.childCount; i++)
            {
                var onOff = transform.GetChild(i).GetComponent<OnOff>();
                if (onOff == null) continue;
                states.Add(onOff);
            }
            return states;
        }

        protected override void OnChildChangedState(OnOff onOff, bool immediate)
        {
            base.OnChildChangedState(onOff, immediate);
            if (onOff.state == Status.On || onOff.state == Status.SwitchingOn)
            {
                SetState(onOff, immediate);
            }
            else if (onOff == currentState && !onOff.isOnSelf)
            {
                SetState(null, immediate);
            }
            else
            {
                AttemptToSwitch();
            }
        }

#if UNITY_EDITOR
		[CanEditMultipleObjects]
        [CustomEditor(typeof(OnOffStateMachine))]
        public class OnOffStateMachineInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                var statemachine = target as OnOffStateMachine;
                if(!statemachine) return;

                StateMachinePopup (statemachine);
		
                base.OnInspectorGUI ();
            }

            public static void StateMachinePopup (OnOffStateMachine statemachine)
            {
                using (var states = statemachine.GetStatesTempList())
                {
                    states.Add(null);
                    
                    var currentId = states.IndexOf(statemachine.currentState);
                    var strings =  states.Select((arg) => arg != null ? new GUIContent(arg.name):new GUIContent("Null")).ToArray();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("Current State");
	                    EditorGUI.BeginChangeCheck();
                        var newId = EditorGUILayout.Popup(currentId, strings, GUILayout.ExpandWidth(false));
                        if(EditorGUI.EndChangeCheck())
                        {
	                        Undo.RegisterFullObjectHierarchyUndo(statemachine, "Setting state to "+strings[newId].text);
                            statemachine.SetState(states[newId], !EditorApplication.isPlaying);
							Undo.FlushUndoRecordObjects();
                        }
                    }
                }		
            }
        }

        [InitializeOnLoad]
        public new class HeirachyWindow 
        {
            static HeirachyWindow()
            {
                EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
            }

            private static void HierarchyWindowItemOnGUI(int instanceid, Rect selectionrect)
            {
                DrawElement(EditorUtility.InstanceIDToObject(instanceid), selectionrect);
            }

            static void DrawElement(Object obj, Rect rect)
            {
                var go = obj as GameObject;
                if (go == null) return;
                var ofsm = go.GetComponent<OnOffStateMachine>();
                if (ofsm == null) return;

                var textOffset = GUI.skin.GetStyle("label").CalcSize(new GUIContent(obj.name));
                var priorWidth = textOffset.x + 20 + OnOff.HeirachyWindow.BUTTTON_WIDTH;
                rect = new Rect(rect.x+priorWidth, rect.y, rect.width-priorWidth, rect.height);
                rect = DrawElement_StateMachine(obj, rect);
            }

            static Rect DrawElement_StateMachine(Object obj, Rect rect)
            {
                var go = obj as GameObject;
                if(!go) return rect;
		
                var statemachine = go.GetComponent<OnOffStateMachine>();
                if(!statemachine) return rect;		
                
                using (var states = statemachine.GetStatesTempList())
                {
                    var width = 40f;
                    foreach (var state in states)
                    {
                        width = Mathf.Max(width, GUI.skin.GetStyle("popup").CalcSize(new GUIContent(state.name)).x);
                    }
                    var popupRect = new Rect(rect.x, rect.y, width, rect.height);
                    states.Add(null);                  
	                if (EditorGUI.DropdownButton(popupRect, new GUIContent(statemachine.currentState !=null?statemachine.currentState.name:"Null"), FocusType.Keyboard))
	                {
		                var overlayRect = popupRect;
		                overlayRect.y -= popupRect.height;
		                PopupWindow.Show(overlayRect, new Popup(statemachine));
	                }
	                
                    rect.x = popupRect.xMax;
                }		
                return rect;
            }

	        public class Popup : PopupWindowContent
	        {
				private OnOffStateMachine statemachine;

				public Popup(OnOffStateMachine statemachine)
				{
					this.statemachine = statemachine;
				}

				public override Vector2 GetWindowSize()
		        {
			        using (var states = statemachine.GetStatesTempList())
			        {
				        var width = 40f;
				        var height = 20f;
				        foreach (var state in states)
				        {
					        var size = GUI.skin.GetStyle("popup").CalcSize(new GUIContent(state.name));
					        width = Mathf.Max(width, size.x+4);
					        height += 20;
				        }
				        return new Vector2(width, height);
			        }
		        }		        

		        public override void OnGUI(Rect rect)
		        {
			        using (var states = statemachine.GetStatesTempList())
			        {
				        states.Add(null);
				        var currentId = states.IndexOf(statemachine.currentState);
				        var newState = statemachine.currentState;
				        var strings =  states.Select((arg) => arg != null ? new GUIContent(arg.name):new GUIContent("Null")).ToArray();
				        for (int i = 0; i < states.Count; i++)
				        {
					        EditorGUI.BeginChangeCheck();							
					        GUI.Toggle(new Rect(rect.x+2, rect.y + i*20+2, rect.width, 16), currentId == i, strings[i]);
							if(EditorGUI.EndChangeCheck())
							{
								newState = states[i];
							}
				        } 

				        if (Event.current.type == EventType.KeyDown || Event.current.type==EventType.ScrollWheel)
				        {
					        int change = 0;
					        if (Event.current.keyCode == KeyCode.UpArrow || Event.current.delta.y < 0) change = -1;
					        else if (Event.current.keyCode == KeyCode.DownArrow || Event.current.delta.y > 0) change = 1;
					        if (change != 0)
					        {
						        newState = states[Mathf.Clamp(currentId + change, 0, states.Count-1)];
						        editorWindow.Repaint();
					        }					        
				        }

				        if (newState != statemachine.currentState)
				        {
					        Undo.RegisterFullObjectHierarchyUndo(statemachine, "Setting state to "+(newState!=null?newState.name:"Null"));
					        statemachine.SetState(newState, !EditorApplication.isPlaying);
					        Undo.FlushUndoRecordObjects();
				        }
			        }		
		        }
	        }
        }
#endif
    }
}