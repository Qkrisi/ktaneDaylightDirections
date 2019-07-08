using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

/// <summary>
/// Module made by hockeygoalie78
/// Based on the condition of the bomb and the day, determine which way to travel.
/// </summary>
public class DaylightDirections : MonoBehaviour {

    public KMBombInfo bombInfo;
    public KMAudio bombAudio;
    public GameObject arrowModel;
    public GameObject leftSun;
    public GameObject rightSun;
    public KMSelectable clockwiseButton;
    public KMSelectable counterClockwiseButton;
    public KMSelectable submitButton;
    public Material[] arrowMaterials;
    public Sprite activeSun;
    public Sprite deactiveSun;
    private KMBombModule bombModule;

    private int materialNumber; //0 is red, 1 is blue, 2 is yellow, 3 is green, 4 is purple
    private int rotation; //0 is directly to the right
    private Vector3 rotationVector;
    private int rightSunActive;

    private string serialNumber;
    private bool serialLastDigitEven;
    private bool serialVowel;
    private bool serialLetterSpecial; //4 letters in the serial number
    private int litIndicatorCount;
    private int aaBatteryCount;
    private int dBatteryCount;
    private int batteryCount;
    private bool containsSpecificPorts; //Contains Serial/DVI-D port but no Parallel ports
    private bool containsDuplicatePort;
    private int solutionRotation;

    private static int moduleIdCounter = 1;
    private int moduleId;

    void Start ()
    {
        //Set module ID
        moduleId = moduleIdCounter++;

        //Delegates for button interactions
        clockwiseButton.OnInteract += delegate { Rotate(true); return false; };
        counterClockwiseButton.OnInteract += delegate { Rotate(false); return false; };
        submitButton.OnInteract += delegate { CheckSolution(); return false; };

        //Set the color of the arrow
        materialNumber = Random.Range(0, 5);
        arrowModel.GetComponent<Renderer>().material = arrowMaterials[materialNumber];

        //Set the initial rotation of the arrow
        rotation = Random.Range(0, 8) * 45;
        rotationVector = new Vector3(0, rotation, 0);
        arrowModel.transform.Rotate(rotationVector);
        rotationVector.y = 45;
        Debug.LogFormat(@"[Daylight Directions #{0}] Starting rotation is {1} degrees. 0 degrees will point to the right.", moduleId, 360 - rotation);

        //Set which sun is active
        rightSunActive = Random.Range(0, 2);
        if(rightSunActive == 1)
        {
            rightSun.GetComponent<SpriteRenderer>().sprite = activeSun;
            leftSun.GetComponent<SpriteRenderer>().sprite = deactiveSun;
        }
        else
        {
            rightSun.GetComponent<SpriteRenderer>().sprite = deactiveSun;
            leftSun.GetComponent<SpriteRenderer>().sprite = activeSun;
        }

        //Serial number
        serialNumber = bombInfo.GetSerialNumber();
        serialLastDigitEven = int.Parse(serialNumber.Substring(5)) % 2 == 0;
        serialVowel = serialNumber.Any("AEIOU".Contains);
        serialLetterSpecial = bombInfo.GetSerialNumberLetters().Count() >= 4;

        //Lit indicators
        litIndicatorCount = bombInfo.GetOnIndicators().Count();

        //Battery counts
        aaBatteryCount = bombInfo.GetBatteryCount(2) + bombInfo.GetBatteryCount(3) + bombInfo.GetBatteryCount(4);
        dBatteryCount = bombInfo.GetBatteryCount(1);
        batteryCount = aaBatteryCount + dBatteryCount;

        //Contains Serial/DVI-D port but no Parallel ports
        containsSpecificPorts = (bombInfo.IsPortPresent("Serial") || bombInfo.IsPortPresent("DVI")) && !bombInfo.IsPortPresent("Parallel");

        //Contains duplicate ports
        containsDuplicatePort = bombInfo.IsDuplicatePortPresent();

        //Other variables as needed
        bombModule = GetComponent<KMBombModule>();

        //Calculate solution
        CalculateSolution();
    }

    /// <summary>
    /// Helper method to rotate the arrow when one of the rotation buttons is pressed
    /// </summary>
    /// <param name="clockwise">True if the button is the clockwise button; false otherwise</param>
    private void Rotate(bool clockwise)
    {
        if(clockwise)
        {
            arrowModel.transform.Rotate(rotationVector);
            rotation = (rotation + 45) % 360;
            clockwiseButton.AddInteractionPunch(.2f);
            bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
            Debug.LogFormat(@"[Daylight Directions #{0}] Rotated arrow clockwise. Rotation is now {1} degrees.", moduleId, (360 - rotation) % 360);
        }
        else
        {
            arrowModel.transform.Rotate(-rotationVector);
            rotation = (rotation + 315) % 360;
            counterClockwiseButton.AddInteractionPunch(.2f);
            bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
            Debug.LogFormat(@"[Daylight Directions #{0}] Rotated arrow counterclockwise. Rotation is now {1} degrees.", moduleId, (360 - rotation) % 360);
        }
    }

    /// <summary>
    /// Calculates the solution of the module
    /// </summary>
    private void CalculateSolution()
    {
        //Direction calculation
        //If the bomb has at least two of the same port, the direction is east
        if(containsDuplicatePort)
        {
            solutionRotation = 0;
            Debug.LogFormat(@"[Daylight Directions #{0}] Duplicate port present. Starting solution rotation is 0 degrees.", moduleId);
        }
        //If the bomb has a Serial or DVI-D port but no Parallel ports, the direction is southwest., the direction is southwest
        else if(containsSpecificPorts)
        {
            solutionRotation = 135;
            Debug.LogFormat(@"[Daylight Directions #{0}] Bomb has a Serial or DVI-D port but no Parallel ports. Starting solution rotation is 225 degrees.", moduleId);
        }
        //If the serial number has four letters, the direction is southeast
        else if(serialLetterSpecial)
        {
            solutionRotation = 45;
            Debug.LogFormat(@"[Daylight Directions #{0}] Serial number has 4 letters. Starting solution rotation is 315 degrees.", moduleId);
        }
        //If the bomb has at least two lit indicators, the direction is northwest
        else if(litIndicatorCount >= 2)
        {
            solutionRotation = 225;
            Debug.LogFormat(@"[Daylight Directions #{0}] At least two lit indicators present. Starting solution rotation is 135 degrees.", moduleId);
        }
        //If the bomb has a D battery, the direction is north
        else if(dBatteryCount >= 1)
        {
            solutionRotation = 270;
            Debug.LogFormat(@"[Daylight Directions #{0}] Bomb has a D battery. Starting solution rotation is 90 degrees.", moduleId);
        }
        //If the serial number has a vowel, the direction is west
        else if(serialVowel)
        {
            solutionRotation = 180;
            Debug.LogFormat(@"[Daylight Directions #{0}] Serial number has a vowel. Starting solution rotation is 180 degrees.", moduleId);
        }
        //If the bomb has more than four batteries, the direction is south
        else if(batteryCount > 4)
        {
            solutionRotation = 90;
            Debug.LogFormat(@"[Daylight Directions #{0}] Bomb has more than four batteries. Starting solution rotation is 270 degrees.", moduleId);
        }
        //Otherwise, the direction is northeast
        else
        {
            solutionRotation = 315;
            Debug.LogFormat(@"[Daylight Directions #{0}] None of the conditions met. Starting solution rotation is 45 degrees.", moduleId);
        }

        //Sun consideration
        if ((rightSunActive == 1 && !serialLastDigitEven) || (rightSunActive == 0 && serialLastDigitEven))
        {
            solutionRotation += 180;
            Debug.LogFormat(@"[Daylight Directions #{0}] Sun conditions have flipped the orientation. Solution rotation is now {1} degrees.", moduleId, Mathf.Abs(360 - solutionRotation) % 360);
        }

        //Material adjustments
        //If the compass arrow is blue, rotate the direction 180 degrees
        if (materialNumber == 1)
        {
            solutionRotation += 180;
            Debug.LogFormat(@"[Daylight Directions #{0}] Arrow is blue, so it rotates 180 degrees. Final solution rotation is {1} degrees.", moduleId, Mathf.Abs(360 - solutionRotation) % 360);
        }
        //If the compass arrow is purple, rotate the direction 45 degrees clockwise
        else if (materialNumber == 4)
        {
            solutionRotation += 45;
            Debug.LogFormat(@"[Daylight Directions #{0}] Arrow is purple, so it rotates 45 degrees clockwise. Final solution rotation is {1} degrees.", moduleId, Mathf.Abs(360 - solutionRotation) % 360);
        }
        //If the compass arrow is green, rotate the direction 135 degrees counterclockwise
        else if (materialNumber == 3)
        {
            solutionRotation += 225; //225 clockwise = 135 counterclockwise
            Debug.LogFormat(@"[Daylight Directions #{0}] Arrow is green, so it rotates 135 degrees counterclockwise. Final solution rotation is {1} degrees.", moduleId, Mathf.Abs(360 - solutionRotation) % 360);
        }
        //If the compass arrow is yellow, rotate the direction 90 degrees clockwise
        else if (materialNumber == 2)
        {
            solutionRotation += 90;
            Debug.LogFormat(@"[Daylight Directions #{0}] Arrow is yellow, so it rotates 90 degrees clockwise. Final solution rotation is {1} degrees.", moduleId, Mathf.Abs(360 - solutionRotation) % 360);
        }
        //Otherwise, don't adjust the direction at all
        else
        {
            Debug.LogFormat(@"[Daylight Directions #{0}] Arrow is red, so no changes occur. Final solution rotation is {1} degrees.", moduleId, Mathf.Abs(360 - solutionRotation) % 360);
        }
        solutionRotation %= 360;
    }

    /// <summary>
    /// Checks to see if the submitted direction is correct and handles the pass or strike accordingly
    /// </summary>
    private void CheckSolution()
    {
        submitButton.AddInteractionPunch(.5f);
        bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);

        //Handle solution comparison to submission
        if(rotation == solutionRotation)
        {
            bombModule.HandlePass();
            Debug.LogFormat(@"[Daylight Directions #{0}] Submitted rotation is correct. Module passed.", moduleId);
        }
        else
        {
            bombModule.HandleStrike();
            Debug.LogFormat(@"[Daylight Directions #{0}] Submitted rotation is incorrect. Strike occurred.", moduleId);
        }
    }
    public string TwitchHelpMessage = "Use '!{0} cw' to rotate clockwise! Use '!{0} ccw' to rotate counterclockwise! Use '{0} submit' to press the submit button!";
    IEnumerator ProcessTwitchCommand(string command)
    {
		string commfinal=command.Replace("press ", "");
		string[] digitstring = commfinal.Split(' ');
		int tried;
		foreach(string option in digitstring){
			if(option=="cw"){
				Debug.LogFormat(@"[Daylight Directions #{0}] Twitch plays command {1} registered; clockwise button pressed.", moduleId, command);
				yield return clockwiseButton;
			}
			if(option=="ccw"){
				Debug.LogFormat(@"[Daylight Directions #{0}] Twitch plays command {1} registered; counterclockwise button pressed.", moduleId, command);
				yield return counterClockwiseButton;
			}
			if(option=="submit"){
				Debug.LogFormat(@"[Daylight Directions #{0}] Twitch plays command {1} registered; submit button pressed.", moduleId, command);
				yield return submitButton;
			}
			
		}
	}
}
