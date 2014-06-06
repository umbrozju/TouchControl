/*
 * Moba Camera Script v1.1 
 * 
 * Notes: 
 * 	- Enabling useFixedUpdate may make camera jumpy when being locked 
 * 	to a target.
 *  - Boundaries dont restrict on the y axis
 * 
 * Plans:
 * 	- Add following of terrain for camera height
 *  - Add ability to have camera look towards a target location
 *  - Add terrain height following
 * 
 * Version:
 * v1.1 
 *  - Removed the boundary list from the MobaCamera script
 *  - Created a separate static class that will contain all boundary and do calculations.
 *  - Created a Boundary component that can be attach to a boundary that will automaticly add it to the boundary list
 *  - Added cube boundaries are able to be rotated on their Y axis
 * 	- Boundaries can now be both cubes and spheres
 *  - Added Axes and Buttons to use the Input Manager instead of KeyCodes
 *  - Added Option to turn on and off use of KeyCodes
 * 
 * v0.5
 *  -Organized Code structure
 * 	-Fixed SetCameraRotation function
 *  -Restrict Camera X rotation on range from -89 to 89
 *  -Added property for currentCameraRotation
 *  -Added property for currentCameraZoomAmount
 *  -Can now set the CameraRotation and CameraZoomAmount at runtime with the
 * corresponding properties
 * 
 * v0.4
 *  -Fixed issue with camera colliding with boundaries when locked to target
 * 
 * v0.3
 * 	-Added boundaries
 * 	-Added defualt height value to camera
 * 	-Allow Camera to Change height value form defult to the locked target's height
 * 
 * v0.2
 * 	-Changed Handling of Player Input with rotation
 *  -Changed Handling of Player Input with zoom
 * 	-fix offset calculation for rotation
 * 	-Added Helper classes for better organization
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using InputHelper;

////////////////////////////////////////////////////////////////////////////////////////////////////
// Helper Classes
[System.Serializable]
public class Moba_Camera_Requirements
{
	// Objects that are requirements for the script to work
	public Transform pivot 	= null;
	public Transform offset = null;
	public Camera camera 	= null;
}

/*
[System.Serializable]
public class Moba_Camera_Inputs 
{
    // Allows camera to be rotated while pressed
    public Vector2 RotateCamera = (0, 0);

    // Toggle lock camera to lockTargetTransform position
    public KeyCode LockCamera = KeyCode.L;

    // Lock camera to lockTargetTransform  position while being pressed
    public KeyCode characterFocus = KeyCode.Space;

    // Move camera based on camera direction
    public KeyCode CameraMoveLeft = KeyCode.LeftArrow;
    public KeyCode CameraMoveRight = KeyCode.RightArrow;
    public KeyCode CameraMoveForward = KeyCode.UpArrow;
    public KeyCode CameraMoveBackward = KeyCode.DownArrow;	
}
*/
[System.Serializable]
public class Moba_Camera_Settings
{	
	// Is the camera restricted to only inside boundaries
	public bool useBoundaries			= true;
	
	// Is the camera locked to a target
	public bool cameraLocked			= false;
	
	// Target for camera to move to when locked
	public Transform lockTargetTransform	= null;
	
	// Helper classes for organization
	public Moba_Camera_Settings_Movement movement = new Moba_Camera_Settings_Movement();
	public Moba_Camera_Settings_Rotation rotation = new Moba_Camera_Settings_Rotation();
	public Moba_Camera_Settings_Zoom zoom = new Moba_Camera_Settings_Zoom();
}

[System.Serializable]
public class Moba_Camera_Settings_Movement {
	// The rate the camera will transition from its current position to target
	public float lockTransitionRate		= 0.1f;

	// How fast the camera moves
	public float cameraMovementRate		= 0.5f;
	
	// Does camera move if mouse is near the edge of the screen
	public bool edgeHoverMovement		= true;
	
	// The Distance from the edge of the screen 
	public float edgeHoverOffset		= 10.0f;
	
	// The defualt value for the height of the pivot y position
	public float defualtHeight			= 0.0f;
	
	// Will set the pivot's y position to the defualtHeight when true
	public bool useDefualtHeight 		= true;
	
	// Uses the lock targets y position when camera locked is true
	public bool useLockTargetHeight		= true;
}

[System.Serializable]
public class Moba_Camera_Settings_Rotation {
	// Zoom rate does not change based on speed of mouse
	public bool constRotationRate		= false;
	
	// Lock the rotations axies
	public bool lockRotationX 			= true;
	public bool lockRotationY 			= true;
	
	// rotation that is used when the game starts
	public Vector2 defualtRotation		= new Vector2(-45.0f, 0.0f);
	
	// How fast the camera rotates
	public Vector2 cameraRotationRate	= new Vector2(100.0f, 100.0f);
}

[System.Serializable]
public class Moba_Camera_Settings_Zoom {
	// Changed direction zoomed
	public bool invertZoom				= false;
	
	// Starting Zoom value
	public float defaultZoom			= 15.0f;
	
	// Minimum and Maximum zoom values
	public float minZoom				= 10.0f;
	public float maxZoom				= 20.0f;
	
	// How fast the camera zooms in and out
	public float zoomRate				= 10.0f;
	
	// Zoom rate does not chance based on scroll speed
	public bool constZoomRate			= false;
}

////////////////////////////////////////////////////////////////////////////////////////////////////
// Moba_Camera Class
public class Moba_Camera : MonoBehaviour 
{	
	// Helper classes
	public Moba_Camera_Requirements requirements	= new Moba_Camera_Requirements();
	public Moba_Camera_Inputs inputs				= new Moba_Camera_Inputs();
	public Moba_Camera_Settings settings			= new Moba_Camera_Settings();
		
	// The Current Zoom value for the camera; Not shown in Inspector
	private float _currentZoomAmount			= 0.0f;
	public float currentZoomAmount 
	{
		get 
		{
			return _currentZoomAmount;
		}
		set 
		{
			_currentZoomAmount = value;
			changeInCamera = true;
		}
	}
	
	// the current Camera Rotation
	private Vector2 _currentCameraRotation 	= Vector3.zero;
	public Vector3 currentCameraRotation 
	{
		get 
		{ 
			return _currentCameraRotation; 
		}
		set 
		{
			_currentCameraRotation = value;
			changeInCamera = true;
		}
	}
	
	// True if either the zoom amount or the rotation value changed
	private bool changeInCamera			= true;
	
	// The amount the mouse has to move before the camera is rotated 
	// Only Used when constRotation rate is true
	private float deltaDeadZone 	= 0.2f;
	
	// Constant values
	private const float MAXROTATIONXAXIS = 89.0f;
	private const float MINROTATIONXAXIS = -89.0f;

	// init the position data
	Vector2 prePos0 = Vector2.zero;
	Vector2 prePos1 = Vector2.zero;

	// init zoom rate
	float zoomlevel = 1.0f;

	const int LayerFloor  =	1 << 8;
	const int LayerPOI 	 =	1 << 9;
	Vector3 RotationCneter = new Vector3(45.0f, 1.8f, -4.6f);

	// Ray cast for zoom in&out from touched point
	bool bCastZoomRay = false;
	bool CastrayToPOI = false;
	bool MoveToPOI = false;
	bool bRotateAlongCenter = false;
	float StartTime = 0.0f;
	float MoveSpeed = 0.5f;
	float MinY = 20.0f;
	float elapseTimeThreadhold = 0.5f; // elapse time threadhold
	static Vector3 midpoint = Vector3.zero;
	static Vector3 hitPosition = Vector3.zero;
	static Vector3 SelectedTargetPos = Vector3.zero;

	// Use this for initialization
	void Start () 
	{
		
		if(!requirements.pivot || !requirements.offset || !requirements.camera) {
			string missingRequirements = "";
			if(requirements.pivot == null) {
				missingRequirements += " / Pivot";
				this.enabled = false;
			}
			
			if(requirements.offset == null) {
				missingRequirements += " / Offset";
				this.enabled = false;
			}
			
			if(requirements.camera == null) {
				missingRequirements += " / Camera";
				this.enabled = false;
			}
			Debug.LogWarning("Moba_Camera Requirements Missing" + missingRequirements + ". Add missing objects to the requirement tab under the Moba_camera script in the Inspector.");
			Debug.LogWarning("Moba_Camera script requires two empty gameobjects, Pivot and Offset, and a camera." +
				"Parent the Offset to the Pivot and the Camera to the Offset. See the Moba_Camera Readme for more information on setup.");
		}
			
		// set values to the defualt values
		_currentZoomAmount 		= settings.zoom.defaultZoom;
		_currentCameraRotation 	= settings.rotation.defualtRotation;
		
		// if using the defualt height
		if(settings.movement.useDefualtHeight && this.enabled) {
			// set the pivots height to the defualt height
			Vector3 tempPos = requirements.pivot.transform.position;
			tempPos.y = settings.movement.defualtHeight;
			requirements.pivot.transform.position = tempPos;
		}
	}
	
	// Update is called once per frame
	void Update() 
    {
        CameraUpdate();	
	}
	
	// Called from Update or FixedUpdate Depending on value of useFixedUpdate
	void CameraUpdate()
	{	
		SelectTarget();
		CalculateCameraZoom();		
		CalculateCameraRotation();		
		CalculateCameraMovement();		
		CalculateCameraUpdates();		
		CalculateCameraBoundaries();
	}

	// Touch to select target
	void SelectTarget ()
	{
		// Keep updating till camera move to right place
		if (requirements.camera.transform.position.y > MinY &&
		    MoveToPOI)
		{
			changeInCamera = true;
		}

		RaycastHit TargetHit;
		int touchcount = Moba_Camera_Inputs.GetTouchCount();
		if ( touchcount != 1) 
		{
			return;
		}
		else
		{
			Moba_Camera_Inputs.TouchEx touch0 = Moba_Camera_Inputs.GetTouch (0);
			Vector3 TouchPoint = new Vector3 (touch0.position.x, touch0.position.y, 0);

			// Record touch begin time once Stationary touchs, but need to reject other type of touches
			if (touch0.phase == TouchPhase.Began  &&
			    StartTime == 0.0f)
			{
				StartTime = Time.time;
			}
			else if(touch0.phase == TouchPhase.Stationary &&
			        StartTime != 0.0f)
			{
				// elapse time big enough
				if ( (Time.time - StartTime) > elapseTimeThreadhold )
				{
					CastrayToPOI = true;
				}
			}
			else if(touch0.phase == TouchPhase.Ended  ||
			        (StartTime != 0 &&  
			 		 touch0.phase != TouchPhase.Stationary))
			{
				StartTime = 0.0f;
			}

			// Do a ray cast
			if ( CastrayToPOI   &&
			    Physics.Raycast(requirements.camera.ScreenPointToRay(TouchPoint), out TargetHit, Mathf.Infinity, LayerPOI))
			{
				CastrayToPOI = false;
				MoveToPOI = true;
				changeInCamera = true;  // Tell the camera update at the first time from here, the rest update lies on the bottom seeting.
				SelectedTargetPos = TargetHit.point;
//				requirements.pivot.position = TargetHit.point;

				Handheld.Vibrate ();
			}
		}
	}

	void CalculateCameraZoom() 
	{
		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Camera Zoom In/Out
		float zoomChange = 0.0f;
		int inverted = 1;

		// two fingers touch gesture
		int touchcount = Moba_Camera_Inputs.GetTouchCount();
		if ( touchcount < 2) 
		{
			return;
		}

		// two finger touches
		Moba_Camera_Inputs.TouchEx touch0;
		Moba_Camera_Inputs.TouchEx touch1;
		touch0 = Moba_Camera_Inputs.GetTouch (0);
		touch1 = Moba_Camera_Inputs.GetTouch (1);

		if(touch0.phase == TouchPhase.Moved &&
		   touch1.phase == TouchPhase.Moved)
		{
			// current position of touches
			Vector2 currentPos0 = touch0.position;
			Vector2 currentPos1 = touch1.position;

			// caculate the midpoint for two figers to caculate the Raycast
			if(midpoint.x == 0)
			{
				midpoint.x =  (currentPos0.x + currentPos1.x) / 2;
				midpoint.y =  (currentPos0.y + currentPos1.y) / 2;
				bCastZoomRay = true;
			}

			//Caculate the gesture
			float preDistant = (prePos0 - prePos1).magnitude;
			float curDistant = (currentPos0 - currentPos1).magnitude;

			// Set the a camera value has changed
			changeInCamera = true;
			
			if(settings.zoom.constZoomRate)
			{
				if(preDistant < curDistant)
				{
					//Zoom In
					zoomChange = 3.0f;
				}
				else
				{
					//Zoom out
					zoomChange = -3.0f;
				}
			}
			else
			{
				zoomChange = curDistant - preDistant;
			}
		
			//change the zoom amount based on if zoom is inverted
			if(!settings.zoom.invertZoom) inverted = -1;
			
			_currentZoomAmount += zoomChange * settings.zoom.zoomRate * inverted * Time.deltaTime;

			// set the new zoom level
			zoomlevel = (_currentZoomAmount + settings.zoom.defaultZoom) / settings.zoom.defaultZoom;

			// Cast Zoom Ray to get the hit point
			if( bCastZoomRay )
			{
				RaycastHit hit;
				
				// Zoom Raycast only happens on floor layer
				if(Physics.Raycast(requirements.camera.ScreenPointToRay(midpoint), out hit, Mathf.Infinity, LayerFloor))
				{
					// Set hitposition
					hitPosition = hit.point;
					
					// Reset bool castray
					bCastZoomRay = false;
					
					// Print info on the screen
					if (guiText == null)
					{
						gameObject.AddComponent<GUIText>();
					}
					
					guiText.text = "Raycast got hit";
				}
				else // missed the hit, do nothing, keep the original pivot
				{
					// midpoint.Set (0, 0, 0);
				}
			}
			// leveve the restore to the rotation phase to aviod zero rotation.
			//prePos0 = currentPos0;
			//prePos1 = currentPos1;
		}
		else
		{
			// Reset the midpoint to zero
			midpoint.Set (0, 0, 0);
		}
	}
	
	void CalculateCameraRotation() 
    {
		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Camera rotate
		float changeInRotationX = 0.0f;
		float changeInRotationY = 0.0f;
		float threshold = 10.0f;

		//two fingers touch gestures
		int touchcount = Moba_Camera_Inputs.GetTouchCount();
		if ( touchcount < 2) 
		{
			return;
		}
		
		// two finger touches
		Moba_Camera_Inputs.TouchEx touch0;
		Moba_Camera_Inputs.TouchEx touch1;
		touch0 = Moba_Camera_Inputs.GetTouch (0);
		touch1 = Moba_Camera_Inputs.GetTouch (1);

		//current position of touches
		Vector2 currentPos0 = touch0.position;
		Vector2 currentPos1 = touch1.position;
		
		if(touch0.phase == TouchPhase.Moved &&
		   touch1.phase == TouchPhase.Moved)
		{
			Vector2 vec0 = prePos1 - prePos0;
			Vector2 vec1 = currentPos1 - currentPos0;
			float diff = Mathf.Abs(vec0.magnitude - vec1.magnitude);

			if ( diff < threshold)
			{
				bRotateAlongCenter = true;

				bool clockwise;
				Vector3 cross = Vector3.Cross (vec0, vec1);

				// determine the roatation guester: counter clockwise or clockwise
				if(cross.z > 0)	clockwise = true;
				else clockwise = false;

				if(!settings.rotation.lockRotationX)
				{
					float deltaVertical = currentPos0.y - prePos0.y;
					if(deltaVertical != 0.0) 
					{
						if(settings.rotation.constRotationRate) 
						{
							if(clockwise) changeInRotationX = -3.0f;
							else changeInRotationX = 3.0f;
						}
						else 
						{
							changeInRotationX = deltaVertical;
						}
						changeInCamera = true;
					}		
				}

				if(!settings.rotation.lockRotationY) 
				{
					float deltaHorizontal = currentPos1.x - prePos1.x;
					if(deltaHorizontal != 0.0f) 
					{
						if(settings.rotation.constRotationRate) 
						{
							if(clockwise) changeInRotationY = 3.0f;
							else  changeInRotationY = -3.0f;
						}
						else 
						{
							changeInRotationY = deltaHorizontal;
						}

						changeInCamera = true;
					}
				}
			}
		}
		else
		{
			bRotateAlongCenter = false; //reset 
		}

		// apply change in Y rotation
		_currentCameraRotation.y += changeInRotationY * settings.rotation.cameraRotationRate.y * Time.deltaTime;
		_currentCameraRotation.x += changeInRotationX * settings.rotation.cameraRotationRate.x * Time.deltaTime;

		// restore the position for next touch
		prePos0 = currentPos0;
		prePos1 = currentPos1;
	}
	
	void CalculateCameraMovement() 
    {
		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Camera Movement : When mouse is near the screens edge

		int touchcount = Moba_Camera_Inputs.GetTouchCount();
	
		if ( touchcount <= 0) 
		{
			return;
		}
		
		Moba_Camera_Inputs.TouchEx touch;
		touch = Moba_Camera_Inputs.GetTouch (0);
		// Lock / Unlock camera movement
/*		if ((touch.phase == TouchPhase.Stationary) && 
			(settings.lockTargetTransform != null)) 
		{
			//flip bool value
			settings.cameraLocked = !settings.cameraLocked;	
		}
		
		// if camera is locked or if character focus, set move pivot to target
		if(settings.lockTargetTransform != null && (settings.cameraLocked || 
			((Input.GetKey(inputs.keycodes.characterFocus)):
				(Input.GetButton(inputs.axis.button_char_focus)))))
		{
			Vector3 target = settings.lockTargetTransform.position;
			if((requirements.pivot.position - target).magnitude > 0.2f) {
				if(settings.movement.useDefualtHeight 
					&& !settings.movement.useLockTargetHeight)
				{
					target.y = settings.movement.defualtHeight;	
				}
				else if (!settings.movement.useLockTargetHeight) 
				{
					target.y = requirements.pivot.position.y;
				}
				
				// Lerp between the target and current position
				requirements.pivot.position = Vector3.Lerp(requirements.pivot.position, target, settings.movement.lockTransitionRate);
			}	
		}
		else
		{*/
			Vector3 movementVector = new Vector3(0,0,0);

			if ( touchcount == 1 && 
		    	 touch.phase == TouchPhase.Moved) 
			{
				
				// Get movement of the finger since last frame
				Vector2 touchDeltaPosition = touch.deltaPosition;
				
				// Clamp to (-1, 1)
				Vector3 temp = Vector3.zero;
				temp.x = touchDeltaPosition.x / touchDeltaPosition.magnitude;
				temp.y = touchDeltaPosition.y / touchDeltaPosition.magnitude;  

				Vector3 CamVecForward = requirements.camera.transform.TransformDirection(Vector3.up);
				Vector3 CamVecRight = requirements.camera.transform.TransformDirection(Vector3.right);
				
				// movement is invert with touch direction
				movementVector = (- temp.x) * CamVecRight + (- temp.y) * CamVecForward;

				requirements.camera.transform.position += zoomlevel * settings.movement.cameraMovementRate *
														  movementVector * touch.deltaTime;
			}
//		}
	}
	
	void CalculateCameraUpdates() 
	{
		////////////////////////////////////////////////////////////////////////////////////////////////////
		// Update the camera position relative to the pivot if there was a change in the camera transforms
		
		// if there is no change in the camera exit update
		if(!changeInCamera) return;
		
		// Check if the fMaxZoomVal is greater than the fMinZoomVal
		if(settings.zoom.maxZoom < settings.zoom.minZoom)
			settings.zoom.maxZoom = settings.zoom.minZoom + 1;
		
		// Check if Camera Zoom is between the min and max
		if(_currentZoomAmount < settings.zoom.minZoom)
//			_currentZoomAmount = settings.zoom.minZoom;
		if(_currentZoomAmount > settings.zoom.maxZoom)
			_currentZoomAmount = settings.zoom.maxZoom;

		// Restrict rotation X value
		if(_currentCameraRotation.x > MAXROTATIONXAXIS) 
			_currentCameraRotation.x = MAXROTATIONXAXIS;
		else if(_currentCameraRotation.x < MINROTATIONXAXIS)
			_currentCameraRotation.x = MINROTATIONXAXIS;

		// Move camera to selected target smoothly 
		if ( MoveToPOI &&
		    SelectedTargetPos.x != 0)
		{
			if (requirements.camera.transform.position.y > MinY)
			{
				requirements.pivot.position = Vector3.MoveTowards( requirements.pivot.position, SelectedTargetPos, MoveSpeed);
				requirements.camera.transform.position = Vector3.MoveTowards( requirements.camera.transform.position,
				                                                             requirements.pivot.position, MoveSpeed);
			}
			else
			{
				MoveToPOI = false;
				SelectedTargetPos.Set(0, 0 ,0);
			}
		}

		// Move to the Zoom point
		if( hitPosition.x != 0 )
		{
			if (Vector3.Distance(requirements.pivot.position, hitPosition) >  deltaDeadZone)
			{
				// Move the current position to target one
				requirements.pivot.position = Vector3.MoveTowards (requirements.pivot.position, hitPosition, settings.movement.lockTransitionRate);
			}
			else
			{
				hitPosition.Set(0, 0, 0); // Reset hit position
			}
		}

		// Rotate along the center
		if (bRotateAlongCenter)
		{
			float SpeedToCenter = 2.0f;

			if (Vector3.Distance(requirements.pivot.position, RotationCneter) >  deltaDeadZone)
			{
				// move the current position to roatation center
				requirements.pivot.position = Vector3.MoveTowards (requirements.pivot.position, RotationCneter, SpeedToCenter);
			}
			else
			{
				bRotateAlongCenter = false; // Reset
			}
		}

		// Calculate the new position of the camera
		// rotate pivot by the change int camera 
		Vector3 forwardRotation = Quaternion.AngleAxis(_currentCameraRotation.y, Vector3.up) * Vector3.forward;
		requirements.pivot.transform.rotation = Quaternion.LookRotation(forwardRotation);
				
		Vector3 CamVec = requirements.pivot.transform.TransformDirection(Vector3.forward);
		
		// Apply Camera Rotations
		CamVec = Quaternion.AngleAxis(_currentCameraRotation.x, requirements.pivot.transform.TransformDirection(Vector3.right)) * CamVec;
		//CamVec = Quaternion.AngleAxis(_currentCameraRotation.y, Vector3.up) * CamVec;
		
		// Move camera along CamVec by ZoomAmount
		requirements.offset.position = CamVec * _currentZoomAmount + requirements.pivot.position;

		// Make Camera look at the pivot
		requirements.offset.transform.LookAt(requirements.pivot);
		
		// reset the change in camera value to false
		changeInCamera = false;
	}
	
	void CalculateCameraBoundaries() 
	{
/*		if(settings.useBoundaries && !
		  ((inputs.useKeyCodeInputs)?
			(Input.GetKey(inputs.keycodes.CameraMoveRight)):
			(Input.GetButton(inputs.axis.button_camera_move_right)))) 
		{
			// check if the pivot is not in a boundary
			if(!Moba_Camera_Boundaries.isPointInBoundary(requirements.pivot.position)) {
				// Get the closet boundary to the pivot
				Moba_Camera_Boundary boundary = Moba_Camera_Boundaries.GetClosestBoundary(requirements.pivot.position);
				if(boundary != null) {
					// set the pivot's position to the closet point on the boundary
					requirements.pivot.position = Moba_Camera_Boundaries.GetClosestPointOnBoundary(boundary, requirements.pivot.position);
				}
			}
		}*/
	}
	
	//////////////////////////////////////////////////////////////////////////////////////////
	// Class functions
	
	//////////////////////////////////////////////////////////////////////////////////////////
	// Set Variables from outside script
	public void SetTargetTransform(Transform t) {
		if(transform != null) {
			settings.lockTargetTransform = t;	
		}
	}
	
	public void SetCameraRotation(Vector2 rotation) {
		currentCameraRotation = new Vector2(rotation.x, rotation.y);
	}
	
	public void SetCameraRotation(float x, float y) {
		currentCameraRotation = new Vector2(x, y);
	}
	
	public void SetCameraZoom(float amount) {
		currentZoomAmount = amount;
	}
	
	//////////////////////////////////////////////////////////////////////////////////////////
	// Get Variables from outside script
	public Camera GetCamera() {
		return requirements.camera;
	}
}
