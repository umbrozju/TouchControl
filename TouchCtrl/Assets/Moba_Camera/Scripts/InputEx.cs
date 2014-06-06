using UnityEngine;


// Input class
namespace InputHelper
{
	[System.Serializable]
	public class Moba_Camera_Inputs
	{
		public struct TouchEx
		{
			public Vector2 deltaPosition;
			public int fingerId;
			public TouchPhase phase;
			public Vector2 position;
			public int tapCount;
			public float deltaTime;
		}

		private static TouchEx _touchex;

#if UNITY_IPHONE || UNITY_ANDROID
		public static int GetTouchCount()
		{
			return Input.touchCount;
		}

		public static TouchEx GetTouch(int index)
		{
			Touch touch = Input.GetTouch(index);
			
			_touchex.deltaPosition.Set(touch.deltaPosition.x, touch.deltaPosition.y);
			_touchex.fingerId = touch.fingerId;
			_touchex.phase = touch.phase;
			_touchex.position.Set(touch.position.x, touch.position.y);
			_touchex.tapCount = touch.tapCount;
			_touchex.deltaTime = touch.deltaTime;
			
			if (Mathf.Abs(_touchex.deltaPosition.x) <= 1)
			{
				_touchex.deltaPosition.x = 0.0f;
			}
			else if (Mathf.Abs(_touchex.deltaPosition.x) <= 2)
			{
				if (Screen.width > 900)
				{
					_touchex.deltaPosition.x = 0.0f;
				}
			}
			
			if (Mathf.Abs(_touchex.deltaPosition.y) <= 1)
			{
				_touchex.deltaPosition.y = 0.0f;
			}
			else if (Mathf.Abs(_touchex.deltaPosition.y) <= 2)
			{
				if (Screen.width > 900)
				{
					_touchex.deltaPosition.y = 0.0f;
				}
			}

			if (_touchex.phase == TouchPhase.Moved 		   &&
			    Mathf.Abs(_touchex.deltaPosition.x) < 0.5f && 
			    Mathf.Abs(_touchex.deltaPosition.y) < 0.5f)
			{
				_touchex.phase = TouchPhase.Stationary;
			}

			return _touchex;
		}
#else
		public static int GetTouchCount()
		{
			int count = 0;
			
			if (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
			{
				count = 1;
			}
			
			if (Input.GetMouseButton(1) || Input.GetMouseButtonUp(1))
			{
				count = 2;
			}
			
			if (Input.GetMouseButton(2) || Input.GetMouseButtonUp(2))
			{
				count = 3;
			}
			
			return count;
		}

		public static TouchEx GetTouch(int index)
		{
			_touchex.deltaPosition.Set(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
			_touchex.fingerId = index;
			_touchex.phase = TouchPhase.Canceled;
			_touchex.position = Input.mousePosition;
			_touchex.tapCount = 1;
			
			if (Mathf.Abs(_touchex.deltaPosition.x) <= 0.01f)
			{
				_touchex.deltaPosition.x = 0.0f;
			}
			if (Mathf.Abs(_touchex.deltaPosition.y) <= 0.01f)
			{
				_touchex.deltaPosition.y = 0.0f;
			}
			
			if (Input.GetMouseButtonDown(index))
			{
				_touchex.phase = TouchPhase.Began;
			}
			else if (Input.GetMouseButtonUp(index))
			{
				_touchex.phase = TouchPhase.Ended;
			}
			else if (Input.GetMouseButton(index))
			{
				if (_touchex.deltaPosition.x != 0 || _touchex.deltaPosition.y != 0)
				{
					_touchex.phase = TouchPhase.Moved;
				}
				else
				{
					_touchex.phase = TouchPhase.Stationary;
				}
			}
			
			return _touchex;
		}
#endif
	}
}