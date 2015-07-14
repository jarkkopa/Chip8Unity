using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class Chip8Controller : MonoBehaviour
{
	private enum DisplayMode
	{
		Blocks,
		Texture
	}

	[SerializeField]
	private DisplayBlocks displayBlocks;
	[SerializeField]
	private DisplayMode displayMode = DisplayMode.Blocks;
	[SerializeField]
	private DisplayTexture displayTexture;
	[SerializeField]
	private MainMenu mainMenu;
	[SerializeField]
	private List<TextAsset> roms;

	private Chip8 chip8 = new Chip8();
	private IDisplay display;
	private BitArray keys = new BitArray(16);
	private float lastFrame = 0;
	private bool lock60hz = false;
	private int romIndex = -1;
	private bool running = false;
	private SoundPlayer soundPlayer;
	private bool soundsEnabled = true;

	public delegate void RomChangedDelegate(string romName);
	public event RomChangedDelegate RomChangedEvent;

	void Start ()
	{
		soundPlayer = gameObject.GetComponent<SoundPlayer>();

		if (displayMode == DisplayMode.Blocks)
		{
			display = displayBlocks;
			displayTexture.gameObject.SetActive(false);
		}
		else if (displayMode == DisplayMode.Texture)
		{
			display = displayTexture;
			displayBlocks.gameObject.SetActive(false);
		}
		mainMenu.Initialize(this);

		display.CreateScreen();

		ChangeToNextRom();
	}

	void Update()
	{
		chip8.SetKeys(ReadKeys());
		if (running && (lock60hz == false || Time.time - lastFrame >= (1f / 60f)))
		{
			chip8.RunCycle();
			if (chip8.RefreshScreen)
			{
				display.UpdateScreen(chip8.Screen);
				chip8.RefreshScreen = false;
			}

			if (soundsEnabled)
			{
				if (chip8.SoundReg > 0)
					soundPlayer.Play();
				else
					soundPlayer.Stop();
			}
			lastFrame = Time.time;
		}
	}

	public void ChangeToNextRom()
	{
		display.ClearScreen();
		chip8.Initialize();
		TextAsset asset = GetNextRom();
		if (asset != null)
		{
			chip8.LoadIntoMemory(asset.bytes);
			if (RomChangedEvent != null)
				RomChangedEvent(asset.name);
			running = true;
		}
		else
		{
			Debug.LogError("Can't find ROM file!");
		}
	}

	public void Toggle60Hz(bool value)
	{
		lock60hz = value;
	}

	public void ToggleSound(bool value)
	{
		soundsEnabled = value;
		if (soundsEnabled == false)
			soundPlayer.Stop();
	}

	private TextAsset GetNextRom()
	{
		romIndex++;
		if (romIndex >= roms.Count)
			romIndex = 0;
		return roms[romIndex];
	}

	private BitArray ReadKeys()
	{
		keys[0x0] = Input.GetKeyDown(KeyCode.X);
		keys[0x1] = Input.GetKeyDown(KeyCode.Alpha1);
		keys[0x2] = Input.GetKeyDown(KeyCode.Alpha2);
		keys[0x3] = Input.GetKeyDown(KeyCode.Alpha3);
		keys[0x4] = Input.GetKeyDown(KeyCode.Q);
		keys[0x5] = Input.GetKeyDown(KeyCode.W);
		keys[0x6] = Input.GetKeyDown(KeyCode.E);
		keys[0x7] = Input.GetKeyDown(KeyCode.A);
		keys[0x8] = Input.GetKeyDown(KeyCode.S);
		keys[0x9] = Input.GetKeyDown(KeyCode.D);
		keys[0xA] = Input.GetKeyDown(KeyCode.Z);
		keys[0xB] = Input.GetKeyDown(KeyCode.C);
		keys[0xC] = Input.GetKeyDown(KeyCode.Alpha4);
		keys[0xD] = Input.GetKeyDown(KeyCode.R);
		keys[0xE] = Input.GetKeyDown(KeyCode.F);
		keys[0xF] = Input.GetKeyDown(KeyCode.V);
		return keys;
	}
}
