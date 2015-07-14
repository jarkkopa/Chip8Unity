using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
	[SerializeField]
	private Button changeRomButton;
	[SerializeField]
	private Button helpButton;
	[SerializeField]
	private GameObject helpPanel;
	[SerializeField]
	private Text romNameText;
	[SerializeField]
	private Toggle toggle60Hz;
	[SerializeField]
	private Toggle toggleSound;

	private Chip8Controller main;

	void Start ()
	{
		helpPanel.gameObject.SetActive(false);
	}

	void OnDestroy()
	{
		main.RomChangedEvent -= this.OnRomChanged;
		helpButton.onClick.RemoveListener(this.OnHelpButtonClicked);
		changeRomButton.onClick.RemoveListener(this.OnRomButtonClicked);
		toggle60Hz.onValueChanged.RemoveListener(this.OnToggle60HzClicked);
		toggleSound.onValueChanged.RemoveListener(this.OnToggleSoundClicked);
	}

	public void Initialize(Chip8Controller main)
	{
		this.main = main;
		main.RomChangedEvent += this.OnRomChanged;
		helpButton.onClick.AddListener(this.OnHelpButtonClicked);
		changeRomButton.onClick.AddListener(this.OnRomButtonClicked);
		toggle60Hz.onValueChanged.AddListener(this.OnToggle60HzClicked);
		toggleSound.onValueChanged.AddListener(this.OnToggleSoundClicked);
	}

	private void OnHelpButtonClicked()
	{
		helpPanel.gameObject.SetActive(!helpPanel.gameObject.activeSelf);
	}

	private void OnRomButtonClicked()
	{
		main.ChangeToNextRom();
	}

	private void OnRomChanged(string romName)
	{
		romNameText.text = romName;
	}

	private void OnToggle60HzClicked(bool value)
	{
		main.Toggle60Hz(value);
	}

	private void OnToggleSoundClicked(bool value)
	{
		main.ToggleSound(value);
	}
}
